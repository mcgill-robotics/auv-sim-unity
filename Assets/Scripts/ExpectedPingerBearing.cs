using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class ExpectedBearingBearing : MonoBehaviour {
	ROSConnection roscon;
	string pingerBearingTopicName = "/sensors/hydrophones/pinger_bearing";
	PingerTimeDifference pingerTimeDifference;

	public Transform Diana;
	public Transform expectedBearing1;
	public Transform expectedBearing2;
	public Transform expectedBearing3;
	public Transform expectedBearing4;
	List<Transform> bearingsList = new List<Transform>();

	int[] frequencies = new int[4];

	Quaternion default_rotation = new Quaternion(-0.7071068f, 0f, 0f, 0.7071068f);

	void Start() {
		pingerTimeDifference = FindObjectOfType<PingerTimeDifference>(); // Find an instance of the other class
		if (pingerTimeDifference == null) {
			Debug.LogError("[in ExpectedPingerBearing.cs] pingerTimeDifference class is not assigned.");
		}
		
		frequencies = pingerTimeDifference.frequencies;
		
		bearingsList.Add(expectedBearing1);
		bearingsList.Add(expectedBearing2);
		bearingsList.Add(expectedBearing3);
		bearingsList.Add(expectedBearing4);

		roscon = ROSConnection.GetOrCreateInstance();
		roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);
	}

	void pingerBearingCallback(PingerBearingMsg msg) {
		// unity is ROS
		// y is z
		// x is -y
		// z is x
		int frequency_index = Array.IndexOf(frequencies, msg.frequency);
		
		if (frequency_index == -1) {
			Debug.LogError("Frequency received mismatch error.");
			return;
		} 

		bearingsList[frequency_index].position = new Vector3(
			-(float)msg.pinger_bearing.y, 
			(float)msg.pinger_bearing.z, 
			(float)msg.pinger_bearing.x
		);
	}
}


