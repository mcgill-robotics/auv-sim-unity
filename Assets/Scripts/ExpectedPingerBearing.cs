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

    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;

    bool hasReceivedPingerEstimate = false;

    public Transform Diana;

    public Transform ExpectedPinger1;
    public Transform ExpectedPinger2;
    public Transform ExpectedPinger3;
    public Transform ExpectedPinger4;

    public GameObject truePingerBearingParent;

    Quaternion default_rotation = new Quaternion(-0.7071068f, 0f, 0f, 0.7071068f);

    void pingerBearingCallback(PingerBearingMsg msg) {
        // unity is ROS
        // y is z
        // x is -y
        // z is x
        
        pinger1Bearing = new Vector3(-(float)msg.pinger1_bearing.y, 0.0f, (float)msg.pinger1_bearing.x);
        pinger2Bearing = new Vector3(-(float)msg.pinger2_bearing.y, 0.0f, (float)msg.pinger2_bearing.x);
        pinger3Bearing = new Vector3(-(float)msg.pinger3_bearing.y, 0.0f, (float)msg.pinger3_bearing.x);
        pinger4Bearing = new Vector3(-(float)msg.pinger4_bearing.y, 0.0f, (float)msg.pinger4_bearing.x);
        hasReceivedPingerEstimate = true;
        truePingerBearingParent.SetActive(true);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);
    }

    void Update() {
        if (!hasReceivedPingerEstimate) return;
        ExpectedPinger1.position = Diana.position + new Vector3(0,1,0);
        ExpectedPinger2.position = Diana.position + new Vector3(0,1,0);
        ExpectedPinger3.position = Diana.position + new Vector3(0,1,0);
        ExpectedPinger4.position = Diana.position + new Vector3(0,1,0);
        
        ExpectedPinger1.rotation = Quaternion.LookRotation(pinger1Bearing) * default_rotation;
        ExpectedPinger2.rotation = Quaternion.LookRotation(pinger2Bearing) * default_rotation;
        ExpectedPinger3.rotation = Quaternion.LookRotation(pinger3Bearing) * default_rotation;
        ExpectedPinger4.rotation = Quaternion.LookRotation(pinger4Bearing) * default_rotation;
    }
}


