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
	private ROSConnection roscon;

	LogicManager1 classLogicManager;
	PingerTimeDifference classPingerTimeDifference;
	int numberOfPingers = 4;
	public string stateTopicName = "/unity/state";
	public GameObject auv;
	public Rigidbody auvRb;
	Vector3 acceleration;
	Vector3 lastVelocity;
	Vector3 currentVelocity;

	private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();
	private float timeSinceLastPublish;
	bool isDVLActive;
	bool isDepthSensorActive;
	bool isIMUActive;
	bool isHydrophonesActive;
	bool publishToRos;
	float publishRate = 0.1f;
	
	uint[][] times;
	int[] frequencies;

	// Start is called before the first frame update
	void Start()
	{
		lastVelocity = auvRb.velocity;
		acceleration = new Vector3(0, 0, 0);
		times = new uint[numberOfPingers][];
		frequencies = new int[numberOfPingers];
		
		classLogicManager = FindObjectOfType<LogicManager1>(); // Find an instance of the other class
		if (classLogicManager != null)
		{
			SubscribeToggle(classLogicManager.PublishDVLToggle, UpdateisDVLActive);
			SubscribeToggle(classLogicManager.PublishROSToggle, UpdatePublishToRos);
			SubscribeToggle(classLogicManager.PublishDepthToggle, UpdateisDepthSensorActive);
			SubscribeToggle(classLogicManager.PublishIMUToggle, UpdateisIMUActive);
			SubscribeToggle(classLogicManager.PublishHydrophonesToggle, UpdateisHydrophonesActive);
		}
		else
		{
			Debug.LogError("[in StatePublisher.cs] LogiManager class is not assigned.");
		}

		classPingerTimeDifference = FindObjectOfType<PingerTimeDifference>(); ;
		if (classPingerTimeDifference == null)
		{
			Debug.LogError("[in StatePublisher.cs] PingerTimeDifference class is not assigned.");
		}
		
		publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
		UpdatePublishRate(int.Parse(PlayerPrefs.GetString("poseRate", "50")));
		
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName);
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

	private void UpdateisDVLActive(bool value)
	{
		isDVLActive = value;
	}
	
	private void UpdatePublishToRos(bool value)
	{
		publishToRos = value;
	}

	public void UpdatePublishRate(int publishFrequency)
	{
		publishRate =  1f / publishFrequency;
	}

	private void UpdateisDepthSensorActive(bool value)
	{
		isDepthSensorActive = value;
	}

	private void UpdateisIMUActive(bool value)
	{
		isIMUActive = value;
	}

	private void UpdateisHydrophonesActive(bool value)
	{
		isHydrophonesActive = value;
	}

	void OnDestroy()
	{
		// Unsubscribe from the events to prevent memory leaks.
		UnsubscribeToggle(classLogicManager.PublishDVLToggle, UpdateisDVLActive);
		UnsubscribeToggle(classLogicManager.PublishROSToggle, UpdatePublishToRos);
		UnsubscribeToggle(classLogicManager.PublishDepthToggle, UpdateisDepthSensorActive);
		UnsubscribeToggle(classLogicManager.PublishIMUToggle, UpdateisIMUActive);
		UnsubscribeToggle(classLogicManager.PublishHydrophonesToggle, UpdateisHydrophonesActive);
	}

	private void UnsubscribeToggle(Toggle toggle, Action<bool> updateAction)
	{
		if (toggle != null)
		{
			toggle.onValueChanged.RemoveListener((isOn) => updateAction(isOn));
		}
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		// publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
		// publishRate = int.Parse(PlayerPrefs.GetString("poseRate", "50"));

		timeSinceLastPublish += Time.fixedDeltaTime;
		if (!publishToRos || timeSinceLastPublish < publishRate) return;
		timeSinceLastPublish = 0;

		// times = new uint[numberOfPingers][];
		// frequencies = new int[numberOfPingers];
		for (int i = 0; i < numberOfPingers; i++)
		{
			(times[i], frequencies[i]) = classPingerTimeDifference.CalculateTimeDifference(i);
		}

		currentVelocity = auvRb.velocity;
		acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
		lastVelocity = currentVelocity;

		msg.position = auv.transform.position.To<RUF>();
		msg.position.y *= -1;

		Quaternion rotation = auv.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
		msg.orientation = rotation.To<NED>();
		msg.angular_velocity = auvRb.angularVelocity.To<RUF>();
		msg.velocity = currentVelocity.To<RUF>();
		msg.frequencies = frequencies;
		msg.times_pinger_1 = times[0];
		msg.times_pinger_2 = times[1];
		msg.times_pinger_3 = times[2];
		msg.times_pinger_4 = times[3];
		msg.linear_acceleration = acceleration.To<RUF>();

		msg.isDVLActive = Convert.ToInt32(isDVLActive);
		msg.isDepthSensorActive = Convert.ToInt32(isDepthSensorActive);
		msg.isIMUActive = Convert.ToInt32(isIMUActive);
		msg.isHydrophonesActive = Convert.ToInt32(isHydrophonesActive);

		roscon.Publish(stateTopicName, msg);
	}
}