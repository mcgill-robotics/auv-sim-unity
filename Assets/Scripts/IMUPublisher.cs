using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

public class IMUPublisher : MonoBehaviour
{
	[Header("IMU Configuration")]
	public string imuTopicName = "/imu/data";
	public GameObject auv;
	public Rigidbody auvRb;
	public ROSClock ROSClock;
	
	[Header("Noise Parameters")]
	[Range(0.001f, 1.0f)]
	public float accelerometerNoise = 0.1f;
	[Range(0.001f, 1.0f)]
	public float gyroscopeNoise = 0.05f;
	[Range(0.001f, 0.1f)]
	public float accelerometerBias = 0.01f;
	[Range(0.001f, 0.1f)]
	public float gyroscopeBias = 0.01f;
	[Range(0.0001f, 0.01f)]
	public float randomWalkStepSize = 0.001f;

	private ROSConnection roscon;
	private LogicManager1 classLogicManager;
	
	private ImuMsg imuMsg = new ImuMsg();
	private float timeSinceLastPublish;
	private bool publishToRos = true;
	private float timeBetweenPublishes = 0.01f; // 100Hz default
	
	// Monte Carlo Random Walk variables
	private Vector3 accelerometerBiasWalk = Vector3.zero;
	private Vector3 gyroscopeBiasWalk = Vector3.zero;
	private Vector3 lastAcceleration = Vector3.zero;
	private Vector3 lastAngularVelocity = Vector3.zero;
	
	// Noise generation
	private System.Random random = new System.Random();
	
	private void Start()
	{
		// Start the ROS connection
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<ImuMsg>(imuTopicName);
		Initialize();
	}

	private void Initialize()
	{
		// Set up IMU message that will be published to ROS
		imuMsg.header = new HeaderMsg();
		imuMsg.header.frame_id = "imu_link";
		
		// Initialize covariance matrices (9x9 flattened)
		imuMsg.orientation_covariance = new double[9];
		imuMsg.angular_velocity_covariance = new double[9];
		imuMsg.linear_acceleration_covariance = new double[9];
		
		SetCovarianceMatrices();
		
		// Subscribe to toggle events
		classLogicManager = FindObjectOfType<LogicManager1>();
		if (classLogicManager != null)
		{
			SubscribeToggle(classLogicManager.PublishIMUToggle, SetPublishToRos);
			SubscribeToggle(classLogicManager.PublishROSToggle, SetPublishToRos);
		}
		else
		{
			Debug.LogError("[in IMUPublisher.cs] LogicManager class is not assigned.");
		}
		
		// Update publishing preferences
		var publish = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"))
		              && bool.Parse(PlayerPrefs.GetString("PublishIMUToggle", "true"));
		var frequency = Mathf.Clamp(int.Parse(PlayerPrefs.GetString("imuRate", "100")), 1, 100);
		SetPublishToRos(publish);
		SetPublishRate(frequency);
		
		// Initialize random walk
		InitializeRandomWalk();
	}
	
	private void FixedUpdate()
	{
		timeSinceLastPublish += Time.fixedDeltaTime;
		if (!publishToRos || timeSinceLastPublish < timeBetweenPublishes) return;
		
		// Publishing is enabled and enough time has elapsed since the last publish,
		// so let's publish the next IMU data:
		SendIMUData();
	}
	
	private void OnDestroy()
	{
		// Unsubscribe from the events to prevent memory leaks.
		if (classLogicManager != null)
		{
			UnsubscribeToggle(classLogicManager.PublishIMUToggle, SetPublishToRos);
			UnsubscribeToggle(classLogicManager.PublishROSToggle, SetPublishToRos);
		}
	}

	private void SendIMUData()
	{
		// Update timestamp
		imuMsg.header.stamp.sec = (int)ROSClock.sec;
		imuMsg.header.stamp.nanosec = (int)ROSClock.nanosec;
		
		// Get true orientation from Unity transform
		Quaternion trueOrientation = auv.transform.rotation;
		imuMsg.orientation = trueOrientation.To<FLU>();
		
		// Get true angular velocity from rigidbody (in rad/s)
		Vector3 trueAngularVelocity = auvRb.angularVelocity;
		
		// Get true linear acceleration (including gravity)
		Vector3 trueAcceleration = GetLinearAcceleration();
		
		// Apply Monte Carlo random walk and noise (with reference instead of value)
		ApplyMonteCarloNoise(ref trueAcceleration, ref trueAngularVelocity);
		
		// Convert to ROS coordinate frame and assign
		imuMsg.angular_velocity = trueAngularVelocity.To<FLU>();
		imuMsg.linear_acceleration = trueAcceleration.To<FLU>();
		
		// Publish the IMU message
		roscon.Publish(imuTopicName, imuMsg);
		
		// Reset time until next publish
		timeSinceLastPublish = 0;
	}
	
	private Vector3 GetLinearAcceleration()
	{
		// Calculate acceleration from velocity change
		Vector3 currentVelocity = auvRb.velocity;
		Vector3 acceleration = (currentVelocity - lastAcceleration) / Time.fixedDeltaTime;
		lastAcceleration = currentVelocity;
		
		// Add gravity (IMU measures specific force, not coordinate acceleration)
		// Unity's gravity is typically (0, -9.81, 0)
		Vector3 gravity = Physics.gravity;
		Vector3 specificForce = acceleration - gravity;
		
		return specificForce;
	}
	
	private void ApplyMonteCarloNoise(ref Vector3 acceleration, ref Vector3 angularVelocity)
	{
		// Update random walk bias using Monte Carlo sampling
		UpdateRandomWalkBias();
		
		// Apply bias and white noise to acceleration
		acceleration.x += accelerometerBiasWalk.x + GenerateWhiteNoise() * accelerometerNoise;
		acceleration.y += accelerometerBiasWalk.y + GenerateWhiteNoise() * accelerometerNoise;
		acceleration.z += accelerometerBiasWalk.z + GenerateWhiteNoise() * accelerometerNoise;
		
		// Apply bias and white noise to angular velocity
		angularVelocity.x += gyroscopeBiasWalk.x + GenerateWhiteNoise() * gyroscopeNoise;
		angularVelocity.y += gyroscopeBiasWalk.y + GenerateWhiteNoise() * gyroscopeNoise;
		angularVelocity.z += gyroscopeBiasWalk.z + GenerateWhiteNoise() * gyroscopeNoise;
	}
	
	private void UpdateRandomWalkBias()
	{
		// Monte Carlo random walk for accelerometer bias
		accelerometerBiasWalk.x += GenerateWhiteNoise() * randomWalkStepSize * accelerometerBias;
		accelerometerBiasWalk.y += GenerateWhiteNoise() * randomWalkStepSize * accelerometerBias;
		accelerometerBiasWalk.z += GenerateWhiteNoise() * randomWalkStepSize * accelerometerBias;
		
		// Monte Carlo random walk for gyroscope bias
		gyroscopeBiasWalk.x += GenerateWhiteNoise() * randomWalkStepSize * gyroscopeBias;
		gyroscopeBiasWalk.y += GenerateWhiteNoise() * randomWalkStepSize * gyroscopeBias;
		gyroscopeBiasWalk.z += GenerateWhiteNoise() * randomWalkStepSize * gyroscopeBias;
		
		// Clamp bias values to prevent them from growing unbounded
		accelerometerBiasWalk = Vector3.ClampMagnitude(accelerometerBiasWalk, accelerometerBias * 10f);
		gyroscopeBiasWalk = Vector3.ClampMagnitude(gyroscopeBiasWalk, gyroscopeBias * 10f);
	}
	
	private float GenerateWhiteNoise()
	{
		// Box-Muller transform for Gaussian white noise
		float u1 = (float)random.NextDouble();
		float u2 = (float)random.NextDouble();
		float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
		return randStdNormal;
	}
	
	private void InitializeRandomWalk()
	{
		// Initialize bias walks with small random values
		accelerometerBiasWalk = new Vector3(
			GenerateWhiteNoise() * accelerometerBias * 0.1f,
			GenerateWhiteNoise() * accelerometerBias * 0.1f,
			GenerateWhiteNoise() * accelerometerBias * 0.1f
		);
		
		gyroscopeBiasWalk = new Vector3(
			GenerateWhiteNoise() * gyroscopeBias * 0.1f,
			GenerateWhiteNoise() * gyroscopeBias * 0.1f,
			GenerateWhiteNoise() * gyroscopeBias * 0.1f
		);
	}
	
	private void SetCovarianceMatrices()
	{
		// Set diagonal covariance matrices based on noise parameters
		// Orientation covariance (unknown, set to -1 as per ROS convention)
		for (int i = 0; i < 9; i++)
		{
			imuMsg.orientation_covariance[i] = (i % 4 == 0) ? -1.0 : 0.0;
		}
		
		// Angular velocity covariance
		double gyroVariance = gyroscopeNoise * gyroscopeNoise;
		for (int i = 0; i < 9; i++)
		{
			imuMsg.angular_velocity_covariance[i] = (i % 4 == 0) ? gyroVariance : 0.0;
		}
		
		// Linear acceleration covariance
		double accelVariance = accelerometerNoise * accelerometerNoise; 
		for (int i = 0; i < 9; i++)
		{
			imuMsg.linear_acceleration_covariance[i] = (i % 4 == 0) ? accelVariance : 0.0;
		}
	}
	
	private void SubscribeToggle(Toggle toggle, Action<bool> updateAction)
	{
		if (toggle != null)
		{
			// Initialize with initial toggle value
			updateAction(toggle.isOn);
			
			// Subscribe to the toggle's onValueChanged event
			toggle.onValueChanged.AddListener((isOn) => updateAction(isOn));
		}
		else
		{
			Debug.LogWarning("IMU Toggle is not assigned.");
		}
	}
	
	private void UnsubscribeToggle(Toggle toggle, Action<bool> updateAction)
	{
		if (toggle != null)
		{
			toggle.onValueChanged.RemoveListener((isOn) => updateAction(isOn));
		}
	}
	
	private void SetPublishToRos(bool active)
	{
		publishToRos = active && bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
	}
	
	public void SetPublishRate(int frequency)
	{
		// Clamp frequency to maximum 100Hz as requested
		frequency = Mathf.Clamp(frequency, 1, 100);
		timeBetweenPublishes = 1f / frequency;
		PlayerPrefs.SetString("imuRate", frequency.ToString());
	}
	
	public void SetAccelerometerNoise(float noise)
	{
		accelerometerNoise = Mathf.Clamp(noise, 0.001f, 1.0f);
		SetCovarianceMatrices();
	}
	
	public void SetGyroscopeNoise(float noise)
	{
		gyroscopeNoise = Mathf.Clamp(noise, 0.001f, 1.0f);
		SetCovarianceMatrices();
	}
	
	public void SetRandomWalkStepSize(float stepSize)
	{
		randomWalkStepSize = Mathf.Clamp(stepSize, 0.0001f, 0.01f);
	}
	
	// Public methods for runtime parameter adjustment
	public void SetAccelerometerBias(float bias)
	{
		accelerometerBias = Mathf.Clamp(bias, 0.001f, 0.1f);
	}
	
	public void SetGyroscopeBias(float bias)
	{
		gyroscopeBias = Mathf.Clamp(bias, 0.001f, 0.1f);
	}
}
