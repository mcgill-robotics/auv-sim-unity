using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std.

public class Imu : MonoBehaviour {

    public GameObject auv;
    ROSConnection roscon;

    public string pubImuDataTopic = "/unity/sbg/imu_data";
    public string pubEkfQuatTopic = "/unity/sbg/ekf_quat";

    // public RosMessageTypes.AuvSimUnity.SbgImuDataMsg data_msg;
    // public RosMessageTypes.AuvSimUnity.SbgEkfQuatMsg ekf_msg;

    // Start is called before the first frame update
    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        // roscon.RegisterPublisher<RosMessageTypes.AuvSimUnity.SbgImuDataMsg>(pubImuDataTopic);
        // roscon.RegisterPublisher<RosMessageTypes.AuvSimUnity.SbgEkfQuatMsg>(pubEkfQuatTopic);
    }

    // Update is called once per frame
    void Update() {
        
    }
}