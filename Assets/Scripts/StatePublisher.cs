using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System;
using UnityEngine.Serialization;

public class StatePublisher : MonoBehaviour
{
	LogicManager1 classLogicManager;
	PingerTimeDifference classPingerTimeDifference;
	public string stateTopicName = "/unity/state";
	public GameObject auv;
	public Rigidbody auvRb;

	private ROSConnection roscon;

	private Vector3 acceleration;
	private Vector3 lastVelocity;
	private Vector3 currentVelocity;

	private RosMessageTypes.Auv.UnityStateMsg stateMsg = new RosMessageTypes.Auv.UnityStateMsg();
	private float timeSinceLastPublish;
	private bool is_dvl_active;
	private bool is_depth_sensor_active;
	private bool is_imu_active;
	private bool is_hydrophones_active;
	private bool publishToRos;
	private float timeBetweenPublishes = 0.1f;

	private int numberOfPingers = 4;
	private uint[][] times;
	private int[] frequencies;
	

	private void Start()
	{
		// Start the ROS connection
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName);
		Initialize();
	}

	private void Initialize()
	{
		// Set initial values
		lastVelocity = auvRb.velocity;
		acceleration = new Vector3(0, 0, 0);
		times = new uint[numberOfPingers][];
		frequencies = new int[numberOfPingers];
		
		// Find the PingerTimeDifference class
		classPingerTimeDifference = FindObjectOfType<PingerTimeDifference>(); ;
		if (classPingerTimeDifference == null)
		{
			Debug.LogError("[in StatePublisher.cs] PingerTimeDifference class is not assigned.");
		}
		
		// Subscribe to toggle events
		classLogicManager = FindObjectOfType<LogicManager1>();
		if (classLogicManager != null)
		{
			SubscribeToggle(classLogicManager.PublishDVLToggle, Setis_dvl_active);
			SubscribeToggle(classLogicManager.PublishDepthToggle, Setis_depth_sensor_active);
			SubscribeToggle(classLogicManager.PublishIMUToggle, Setis_imu_active);
			SubscribeToggle(classLogicManager.PublishHydrophonesToggle, Setis_hydrophones_active);
			SubscribeToggle(classLogicManager.PublishROSToggle, SetPublishToRos);
		}
		else
		{
			Debug.LogError("[in StatePublisher.cs] LogiManager class is not assigned.");
		}
		
		// Update publishing preferences and affected variables
		var publish = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
		var frequency = int.Parse(PlayerPrefs.GetString("poseRate", "50"));
		SetPublishToRos(publish);
		SetPublishRate(frequency);
	}
	
	private void FixedUpdate()
	{
		timeSinceLastPublish += Time.fixedDeltaTime;
		if (!publishToRos || timeSinceLastPublish < timeBetweenPublishes) return;
		
		// Publishing is enabled and enough time has elapsed since the last publish,
		// so let's publish the next state:
		SendState();
	}
	
	private void OnDestroy()
	{
		// Unsubscribe from the events to prevent memory leaks.
		UnsubscribeToggle(classLogicManager.PublishDVLToggle, Setis_dvl_active);
		UnsubscribeToggle(classLogicManager.PublishROSToggle, SetPublishToRos);
		UnsubscribeToggle(classLogicManager.PublishDepthToggle, Setis_depth_sensor_active);
		UnsubscribeToggle(classLogicManager.PublishIMUToggle, Setis_imu_active);
		UnsubscribeToggle(classLogicManager.PublishHydrophonesToggle, Setis_hydrophones_active);
	}

	private void SendState()
	{
		// Calculate time differences for each pinger
		for (int i = 0; i < numberOfPingers; i++)
		{
			(times[i], frequencies[i]) = classPingerTimeDifference.CalculateTimeDifference(i);
		}

		// Get the current velocity and acceleration
		currentVelocity = auvRb.velocity;
		acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
		lastVelocity = currentVelocity;

		// Update the state message with the current state
		stateMsg.position = auv.transform.position.To<RUF>();
		stateMsg.position.y *= -1;

		Quaternion rotation = auv.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
		stateMsg.orientation = rotation.To<NED>();
		stateMsg.angular_velocity = auvRb.angularVelocity.To<RUF>();
		stateMsg.velocity = currentVelocity.To<RUF>();
		stateMsg.frequencies = frequencies;
		stateMsg.hydrophone_one_freqs = times[0];
		stateMsg.hydrophone_two_freqs = times[1];
		stateMsg.hydrophone_three_freqs = times[2];
		stateMsg.hydrophone_four_freqs = times[3];
		stateMsg.linear_acceleration = acceleration.To<RUF>();

		stateMsg.is_dvl_active = Convert.ToInt32(is_dvl_active);
		stateMsg.is_depth_sensor_active = Convert.ToInt32(is_depth_sensor_active);
		stateMsg.is_imu_active = Convert.ToInt32(is_imu_active);
		stateMsg.is_hydrophones_active = Convert.ToInt32(is_hydrophones_active);

		roscon.Publish(stateTopicName, stateMsg);
		
		// Reset time until next publish
		timeSinceLastPublish = 0;
	}

	private void SubscribeToggle(Toggle toggle, Action<bool> updateAction)
	{
		if (toggle != null)
		{
			// Initialize isActive[...] var with initial toggle value.
			updateAction(toggle.isOn);

			// Subscribe to the toggle's onValueChanged event.
			toggle.onValueChanged.AddListener((isOn) => updateAction(isOn));
		}
		else
		{
			Debug.LogError("Pub Sensor Toggle is not assigned.");
		}
	}
	
	private void UnsubscribeToggle(Toggle toggle, Action<bool> updateAction)
	{
		if (toggle != null)
		{
			toggle.onValueChanged.RemoveListener((isOn) => updateAction(isOn));
		}
	}

	private void Setis_dvl_active(bool active)
	{
		is_hydrophones_active = active;
	}

	private void Setis_depth_sensor_active(bool active)
	{
		is_depth_sensor_active = active;
	}

	private void Setis_imu_active(bool active)
	{
		is_imu_active = active;
	}

	private void Setis_hydrophones_active(bool active)
	{
		is_hydrophones_active = active;
	}
	
	private void SetPublishToRos(bool active)
	{
		publishToRos = active;
	}
	
	public void SetPublishRate(int frequency)
	{
		timeBetweenPublishes =  1f / frequency;
	}
}