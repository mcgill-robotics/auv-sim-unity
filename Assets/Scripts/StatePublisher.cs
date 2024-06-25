using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System;

public class StatePublisher : MonoBehaviour {
	private ROSConnection roscon;

	LogicManager1 classLogicManager;
	PingerTimeDifference classPingerTimeDifference;
	int numberOfPingers = 4;
	public string stateTopicName = "/unity/state";
	public GameObject auv;
	public Rigidbody auvRigidBody;
    Vector3 acceleration;
    Vector3 lastVelocity;
    Vector3 currentVelocity;

	private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();
	private float timeSinceLastUpdate;
	bool isDVLActive;
	bool isDepthSensorActive;
	bool isIMUActive;
	bool isHydrophonesActive; 
	bool publishToRos;

	int updateFrequency = 10;

	// Start is called before the first frame update
	void Start() {
		lastVelocity = auvRigidBody.velocity;
      	acceleration = new Vector3(0, 0, 0);
		classLogicManager = FindObjectOfType<LogicManager1>(); // Find an instance of the other class
		if (classLogicManager != null) {
			SubscribeToggle(classLogicManager.PublishDVLToggle, UpdateisDVLActive);
			SubscribeToggle(classLogicManager.PublishDepthToggle, UpdateisDepthSensorActive);
			SubscribeToggle(classLogicManager.PublishIMUToggle, UpdateisIMUActive);
			SubscribeToggle(classLogicManager.PublishHydrophonesToggle, UpdateisHydrophonesActive);
		} else {
			Debug.LogError("[in StatePublisher.cs] LogiManager class is not assigned.");
		}

		classPingerTimeDifference = FindObjectOfType<PingerTimeDifference>();;
		if (classPingerTimeDifference == null) {
			Debug.LogError("[in StatePublisher.cs] PingerTimeDifference class is not assigned.");
		}

		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName); 
	}

	private void SubscribeToggle(Toggle toggle, Action<bool> updateAction) {
		if (toggle != null) {
			// Initialize isActive[...] var with initial toggle value.
			updateAction(toggle.isOn);

			// Subscribe to the toggle's onValueChanged event.
			toggle.onValueChanged.AddListener((isOn) => updateAction(isOn));
		} else {
			Debug.LogError("Pub Sensor Toggle is not assigned.");
		}
	}

	private void UpdateisDVLActive(bool value) {
		isDVLActive = value;
	}

	private void UpdateisDepthSensorActive(bool value) {
		isDepthSensorActive = value;
	}

	private void UpdateisIMUActive(bool value) {
		isIMUActive = value;
	}

	private void UpdateisHydrophonesActive(bool value) {
		isHydrophonesActive = value;
	}

	void OnDestroy() {
		// Unsubscribe from the events to prevent memory leaks.
		UnsubscribeToggle(classLogicManager.PublishDVLToggle, UpdateisDVLActive);
		UnsubscribeToggle(classLogicManager.PublishDepthToggle, UpdateisDepthSensorActive);
		UnsubscribeToggle(classLogicManager.PublishIMUToggle, UpdateisIMUActive);
		UnsubscribeToggle(classLogicManager.PublishHydrophonesToggle, UpdateisHydrophonesActive);
	}

	private void UnsubscribeToggle(Toggle toggle, Action<bool> updateAction) {
		if (toggle != null) {
			toggle.onValueChanged.RemoveListener((isOn) => updateAction(isOn));
		}
	}

    // Update is called once per frame
	void FixedUpdate() {
		publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
		updateFrequency = int.Parse(PlayerPrefs.GetString("poseRate", "50"));

		timeSinceLastUpdate += Time.deltaTime;
		if (timeSinceLastUpdate < 1.0/updateFrequency || !publishToRos) {
			return;
		}
		timeSinceLastUpdate = 0;

		uint[][] times = new uint[numberOfPingers][];
		int[] frequencies = new int[numberOfPingers];
		for (int i = 0; i < numberOfPingers; i++) {
			(times[i], frequencies[i]) = classPingerTimeDifference.CalculateTimeDifference(i);
		}
		
		currentVelocity = auvRigidBody.velocity;
		acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
		lastVelocity = currentVelocity;
		
		msg.position = auv.transform.position.To<RUF>();

		Quaternion rotation = auv.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
		msg.orientation = rotation.To<NED>();
		msg.angular_velocity = auv.GetComponent<Rigidbody>().angularVelocity.To<RUF>();
		msg.velocity = auv.GetComponent<Rigidbody>().velocity.To<RUF>();
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