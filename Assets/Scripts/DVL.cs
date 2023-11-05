using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public class DVLPublisher : MonoBehaviour {
   ROSConnection roscon;
   public string topicName = "/dead_reckon_report";

    public GameObject auv;

    public float x;
    public float y;
    public float z;
    public float roll;
    public float pitch;
    public float yaw;

   public RosMessageTypes.Auv.DeadReckonReportMsg msg;


    // Start is called before the first frame update
    void Start() {
      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.DeadReckonReportMsg>(topicName); 
    }

    // Update is called once per frame
    void Update() {
      x = auv.transform.position.x;
      y = auv.transform.position.y;
      z = auv.transform.position.z;
      roll = auv.transform.rotation.eulerAngles.x;
      pitch = auv.transform.rotation.eulerAngles.y;
      yaw = auv.transform.rotation.eulerAngles.z;
    }
}
