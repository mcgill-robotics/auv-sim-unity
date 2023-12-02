using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class StatePublisher : MonoBehaviour {
    private ROSConnection roscon;
    public string stateTopicName = "/unity/state";
    public bool isDVLActive = true;
    public bool isDepthSensorActive = true;
    public bool isIMUActive = true;

    public GameObject auv;

    private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();

    // Start is called before the first frame update
    void Start() {
      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName); 
    }

    // Update is called once per frame
    void Update() {
      msg.position = auv.transform.position.To<NED>();
      msg.orientation = auv.transform.rotation.To<NED>();
      msg.velocity = auv.GetComponent<Rigidbody>().velocity.To<NED>();
      msg.angular_velocity = auv.GetComponent<Rigidbody>().angularVelocity.To<NED>();
      msg.isDVLActive = isDVLActive;
      msg.isDepthSensorActive = isDepthSensorActive;
      msg.isIMUActive = isIMUActive;

      roscon.Publish(stateTopicName, msg);
    }
}
