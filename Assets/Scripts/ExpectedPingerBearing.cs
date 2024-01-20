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
    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;

    public Transform Diana;

    public Transform ExpectedPinger1;
    public Transform ExpectedPinger2;
    public Transform ExpectedPinger3;
    public Transform ExpectedPinger4;

    void pingerBearingCallback(PingerBearingMsg msg) {
        pinger1Bearing = new Vector3((float)msg.pinger1_bearing.z, (float)msg.pinger1_bearing.x, (float)msg.pinger1_bearing.y);
        pinger2Bearing = new Vector3((float)msg.pinger2_bearing.z, (float)msg.pinger2_bearing.x, (float)msg.pinger2_bearing.y);
        pinger3Bearing = new Vector3((float)msg.pinger3_bearing.z, (float)msg.pinger3_bearing.x, (float)msg.pinger3_bearing.y);
        pinger4Bearing = new Vector3((float)msg.pinger4_bearing.z, (float)msg.pinger4_bearing.x, (float)msg.pinger4_bearing.y);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);
    }

    void Update() {
        ExpectedPinger1.position = Vector3.MoveTowards(ExpectedPinger1.position, Diana.position, 5.0f);
        ExpectedPinger2.position = Vector3.MoveTowards(ExpectedPinger2.position, Diana.position, 5.0f);
        ExpectedPinger3.position = Vector3.MoveTowards(ExpectedPinger3.position, Diana.position, 5.0f);
        ExpectedPinger4.position = Vector3.MoveTowards(ExpectedPinger4.position, Diana.position, 5.0f);
        var lookPos1 = pinger1Bearing;
        var lookPos2 = pinger2Bearing;
        var lookPos3 = pinger3Bearing;
        var lookPos4 = pinger4Bearing;
        lookPos1.y = 0;
        lookPos2.y = 0;
        lookPos3.y = 0;
        lookPos4.y = 0;
        var rotation1 = Quaternion.LookRotation(lookPos1);
        var rotation2 = Quaternion.LookRotation(lookPos2);
        var rotation3 = Quaternion.LookRotation(lookPos3);
        var rotation4 = Quaternion.LookRotation(lookPos4);
        ExpectedPinger1.rotation = Quaternion.Slerp(ExpectedPinger1.rotation, rotation1, Time.deltaTime * damping);
        ExpectedPinger2.rotation = Quaternion.Slerp(ExpectedPinger2.rotation, rotation2, Time.deltaTime * damping);
        ExpectedPinger3.rotation = Quaternion.Slerp(ExpectedPinger3.rotation, rotation3, Time.deltaTime * damping);
        ExpectedPinger4.rotation = Quaternion.Slerp(ExpectedPinger4.rotation, rotation4, Time.deltaTime * damping);
    }
}


