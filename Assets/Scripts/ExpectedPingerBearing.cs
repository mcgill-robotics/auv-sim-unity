using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class ExpectedPingerBearing : MonoBehaviour {
    ROSConnection roscon;
    private string pingerBearingTopicName = "/sensors/hydrophones/pinger_bearing";
    PingerTimeDifference pingerTimeDifference;

    Vector3[] pingerBearing = new Vector3[4];

    bool[] hasReceivedPingerEstimate = {false, false, false, false};

    public Transform Diana;

    public Transform ExpectedPinger1;
    public Transform ExpectedPinger2;
    public Transform ExpectedPinger3;
    public Transform ExpectedPinger4;
    Transform[] ExpectedPinger = new Transform[4];
 
    public GameObject truePingerBearingParent;
    int[] frequencies = new int[4];

    Quaternion default_rotation = new Quaternion(-0.7071068f, 0f, 0f, 0.7071068f);

    void pingerBearingCallback(PingerBearingMsg msg) {
        // unity is ROS
        // y is z
        // x is -y
        // z is x
        int frequency_index = Array.IndexOf(frequencies, msg.frequency);
        pingerBearing[frequency_index] = new Vector3(
            -(float)msg.pinger_bearing.y, 
            (float)msg.pinger_bearing.z, 
            (float)msg.pinger_bearing.x
        );

        hasReceivedPingerEstimate[frequency_index] = true;
        truePingerBearingParent.SetActive(true);
    }

    void Start() {
        pingerTimeDifference = FindObjectOfType<PingerTimeDifference>(); // Find an instance of the other class
        frequencies = pingerTimeDifference.frequencies;
        ExpectedPinger[0] = ExpectedPinger1; 
        ExpectedPinger[1] = ExpectedPinger2; 
        ExpectedPinger[2] = ExpectedPinger3; 
        ExpectedPinger[3] = ExpectedPinger4;
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);
    }

    void Update() {
        for (int i = 0; i < ExpectedPinger.Length; i++) {
            if (hasReceivedPingerEstimate[i]) {
                ExpectedPinger[i].position = Diana.position + new Vector3(0,1,0);
                ExpectedPinger[i].rotation = Quaternion.LookRotation(pingerBearing[i]) * default_rotation;
            }
        }
    }
}


