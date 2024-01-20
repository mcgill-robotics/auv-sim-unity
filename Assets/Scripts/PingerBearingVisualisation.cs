using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class PingerBearingVisualisation : MonoBehaviour {
    ROSConnection roscon;
    private string pingerBearingTopicName = "/sensors/hydrophones/pinger_bearing";

    public Transform Diana;

    public Transform hydrophone1;
    public Transform hydrophone2;
    public Transform hydrophone3;
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;
    int damping = 5;

    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;

    void pingerBearingCallback(PingerBearingMsg msg) {
        pinger1Bearing = new Vector3((float)msg.pinger1_bearing.x, (float)msg.pinger1_bearing.y, (float)msg.pinger1_bearing.z);
        pinger2Bearing = new Vector3((float)msg.pinger2_bearing.x, (float)msg.pinger2_bearing.y, (float)msg.pinger2_bearing.z);
        pinger3Bearing = new Vector3((float)msg.pinger3_bearing.x, (float)msg.pinger3_bearing.y, (float)msg.pinger3_bearing.z);
        pinger4Bearing = new Vector3((float)msg.pinger4_bearing.x, (float)msg.pinger4_bearing.y, (float)msg.pinger4_bearing.z);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(pingerBearingTopicName, pingerBearingCallback);

        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger1.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }

    void Update() {
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger1.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }
}