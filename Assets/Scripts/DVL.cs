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
      msg.x = auv.transform.position.x;
      msg.y = auv.transform.position.y;
      msg.z = auv.transform.position.z;
      msg.roll = auv.transform.rotation.eulerAngles.x;
      msg.pitch = auv.transform.rotation.eulerAngles.y;
      msg.yaw = auv.transform.rotation.eulerAngles.z;
      roscon.Publish(topicName, msg);
    }
}
