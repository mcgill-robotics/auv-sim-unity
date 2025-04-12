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
	private bool isDVLActive;
	private bool isDepthSensorActive;
	private bool isIMUActive;
	private bool isHydrophonesActive;
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
			SubscribeToggle(classLogicManager.PublishDVLToggle, SetisDVLActive);
			SubscribeToggle(classLogicManager.PublishDepthToggle, SetisDepthSensorActive);
			SubscribeToggle(classLogicManager.PublishIMUToggle, SetisIMUActive);
			SubscribeToggle(classLogicManager.PublishHydrophonesToggle, SetisHydrophonesActive);
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
		UnsubscribeToggle(classLogicManager.PublishDVLToggle, SetisDVLActive);
		UnsubscribeToggle(classLogicManager.PublishROSToggle, SetPublishToRos);
		UnsubscribeToggle(classLogicManager.PublishDepthToggle, SetisDepthSensorActive);
		UnsubscribeToggle(classLogicManager.PublishIMUToggle, SetisIMUActive);
		UnsubscribeToggle(classLogicManager.PublishHydrophonesToggle, SetisHydrophonesActive);
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
		stateMsg.times_pinger_1 = times[0];
		stateMsg.times_pinger_2 = times[1];
		stateMsg.times_pinger_3 = times[2];
		stateMsg.times_pinger_4 = times[3];
		stateMsg.linear_acceleration = acceleration.To<RUF>();

		stateMsg.isDVLActive = Convert.ToInt32(isDVLActive);
		stateMsg.isDepthSensorActive = Convert.ToInt32(isDepthSensorActive);
		stateMsg.isIMUActive = Convert.ToInt32(isIMUActive);
		stateMsg.isHydrophonesActive = Convert.ToInt32(isHydrophonesActive);

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

	private void SetisDVLActive(bool active)
	{
		isDVLActive = active;
	}

	private void SetisDepthSensorActive(bool active)
	{
		isDepthSensorActive = active;
	}

	private void SetisIMUActive(bool active)
	{
		isIMUActive = active;
	}

	private void SetisHydrophonesActive(bool active)
	{
		isHydrophonesActive = active;
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