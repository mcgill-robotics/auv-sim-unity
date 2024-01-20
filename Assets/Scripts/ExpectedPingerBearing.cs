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

    public Vector3 pinger1Bearing;
    public Vector3 pinger2Bearing;
    public Vector3 pinger3Bearing;
    public Vector3 pinger4Bearing;

    public Vector3 = new Vector3(-1.0f, 1.0f, 0.0f);

    public Transform Diana;

    public Transform ExpectedPinger1;
    public Transform ExpectedPinger2;
    public Transform ExpectedPinger3;
    public Transform ExpectedPinger4;

    void pingerBearingCallback(PingerBearingMsg msg) {
        // unity is ROS
        // y is z
        // x is -y
        // z is x
        pinger1Bearing = new Vector3(-(float)msg.pinger1_bearing.y, 0.0f, (float)msg.pinger1_bearing.x);
        pinger2Bearing = new Vector3(-(float)msg.pinger2_bearing.y, 0.0f, (float)msg.pinger2_bearing.x);
        pinger3Bearing = new Vector3(-(float)msg.pinger3_bearing.y, 0.0f, (float)msg.pinger3_bearing.x);
        pinger4Bearing = new Vector3(-(float)msg.pinger4_bearing.y, 0.0f, (float)msg.pinger4_bearing.x);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);
    }

    void Update() {
        ExpectedPinger1.position = Diana.position;
        ExpectedPinger2.position = Diana.position;
        ExpectedPinger3.position = Diana.position;
        ExpectedPinger4.position = Diana.position;

        Debug.Log("Diana.rotation: " + Diana.rotation);
        
        ExpectedPinger1.rotation = Quaternion.LookRotation(pinger1Bearing);
        ExpectedPinger2.rotation = Quaternion.LookRotation(pinger2Bearing);
        ExpectedPinger3.rotation = Quaternion.LookRotation(pinger3Bearing);
        ExpectedPinger4.rotation = Quaternion.LookRotation(pinger4Bearing);
    }
}


