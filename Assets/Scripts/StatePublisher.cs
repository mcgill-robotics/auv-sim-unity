using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class StatePublisher : MonoBehaviour {
    private ROSConnection roscon;
    public string stateTopicName = "/unity/state";

    public GameObject auv;

    private RosMessageTypes.Auv.UnityStateMsg msg;

    // Start is called before the first frame update
    void Start() {
      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName); 
    }

    // Update is called once per frame
    void Update() {
      msg.position = auv.transform.position.To<FLU>();
      msg.orientation = auv.transform.rotation.To<FLU>();
      msg.velocity = auv.GetComponent<Rigidbody>().velocity.To<FLU>();
      msg.angular_velocity = auv.GetComponent<Rigidbody>().angularVelocity.To<FLU>();

      roscon.Publish(stateTopicName, msg);
    }
}
