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

    int damping = 5;
    public Vector3 pinger1Bearing;
    public Vector3 pinger2Bearing;
    public Vector3 pinger3Bearing;
    public Vector3 pinger4Bearing;

    public Transform Diana;

    public Transform ExpectedPinger1;
    public Transform ExpectedPinger2;
    public Transform ExpectedPinger3;
    public Transform ExpectedPinger4;

    void pingerBearingCallback(PingerBearingMsg msg) {
        pinger1Bearing = new Vector3(-(float)msg.pinger1_bearing.y, (float)msg.pinger1_bearing.z, (float)msg.pinger1_bearing.x);
        pinger2Bearing = new Vector3(-(float)msg.pinger2_bearing.y, (float)msg.pinger2_bearing.z, (float)msg.pinger2_bearing.x);
        pinger3Bearing = new Vector3(-(float)msg.pinger3_bearing.y, (float)msg.pinger3_bearing.z, (float)msg.pinger3_bearing.x);
        pinger4Bearing = new Vector3(-(float)msg.pinger4_bearing.y, (float)msg.pinger4_bearing.z, (float)msg.pinger4_bearing.x);
    }

    // unity is ROS
    // y is z
    // x is -y
    // z is x

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);
    }

    void Update() {
        ExpectedPinger1.position = Vector3.MoveTowards(ExpectedPinger1.position, Diana.position, 5.0f);
        ExpectedPinger2.position = Vector3.MoveTowards(ExpectedPinger2.position, Diana.position, 5.0f);
        ExpectedPinger3.position = Vector3.MoveTowards(ExpectedPinger3.position, Diana.position, 5.0f);
        ExpectedPinger4.position = Vector3.MoveTowards(ExpectedPinger4.position, Diana.position, 5.0f);

        Debug.Log("BEFORE pinger1Bearing: " + pinger1Bearing);
        pinger1Bearing.y = 0;
        pinger2Bearing.y = 0;
        pinger3Bearing.y = 0;
        pinger4Bearing.y = 0;
        Debug.Log("AFTER pinger1Bearing: " + pinger1Bearing);
        var rotation1 = Quaternion.LookRotation(pinger1Bearing);
        var rotation2 = Quaternion.LookRotation(pinger2Bearing);
        var rotation3 = Quaternion.LookRotation(pinger3Bearing);
        var rotation4 = Quaternion.LookRotation(pinger4Bearing);

        // Debug.Log("pinger1Bearing: " + pinger1Bearing);
        // Debug.Log("rotation1: " + rotation1);
        // Debug.Log("ExpectedPinger1.rotation: " + ExpectedPinger1.rotation);
        
        ExpectedPinger1.rotation = Quaternion.Slerp(ExpectedPinger1.rotation, rotation1, Time.deltaTime * damping);
        ExpectedPinger2.rotation = Quaternion.Slerp(ExpectedPinger2.rotation, rotation2, Time.deltaTime * damping);
        ExpectedPinger3.rotation = Quaternion.Slerp(ExpectedPinger3.rotation, rotation3, Time.deltaTime * damping);
        ExpectedPinger4.rotation = Quaternion.Slerp(ExpectedPinger4.rotation, rotation4, Time.deltaTime * damping);
    }
}


