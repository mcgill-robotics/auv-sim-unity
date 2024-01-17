using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class PingerBearing : MonoBehaviour {
    ROSConnection roscon;
    private string pinger1TopicName = "/sensors/hydrophones/pinger1_bearing";
    private string pinger2TopicName = "/sensors/hydrophones/pinger2_bearing";
    private string pinger3TopicName = "/sensors/hydrophones/pinger3_bearing";
    private string pinger4TopicName = "/sensors/hydrophones/pinger4_bearing";

    public Transform pinger1;
    public Transform Diana;

    int damping = 5;

    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;

    void pinger1Callback(Float64MultiArrayMsg pinger1) {
        pinger1Bearing = new Vector3((float)pinger1.data[0], (float)pinger1.data[1], (float)pinger1.data[2]);
    }

    void pinger2Callback(Float64MultiArrayMsg pinger2) {
        pinger2Bearing = new Vector3((float)pinger2.data[0], (float)pinger2.data[1], (float)pinger2.data[2]);
    }

    void pinger3Callback(Float64MultiArrayMsg pinger3) {
        pinger3Bearing = new Vector3((float)pinger3.data[0], (float)pinger3.data[1], (float)pinger3.data[2]);
    }

    void pinger4Callback(Float64MultiArrayMsg pinger4) {
        pinger4Bearing = new Vector3((float)pinger4.data[0], (float)pinger4.data[1], (float)pinger4.data[2]);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<Float64MultiArrayMsg>(pinger1TopicName, pinger1Callback);
        roscon.Subscribe<Float64MultiArrayMsg>(pinger2TopicName, pinger2Callback);
        roscon.Subscribe<Float64MultiArrayMsg>(pinger3TopicName, pinger3Callback);
        roscon.Subscribe<Float64MultiArrayMsg>(pinger4TopicName, pinger4Callback);

        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger1.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }

    void Update()  {
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger1.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }
}