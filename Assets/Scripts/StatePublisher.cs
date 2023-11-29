using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class StatePublisher : MonoBehaviour {
    private ROSConnection roscon;
    public string stateTopicName = "/unity/state";

    public GameObject auv;

    private RosMessageTypes.Auv.UnityState msg;

    // Start is called before the first frame update
    void Start() {
      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.UnityState>(stateTopicName); 
    }

    // Update is called once per frame
    void Update() {
      msg.pose = new Pose
        {
            position = new Point { x = auv.transform.position.x, y = auv.transform.position.y, z = auv.transform.position.z }, 
            orientation = new Quaternion { x = auv.transform.rotation.x, y = auv.transform.rotation.y, z = auv.transform.rotation.z, w = auv.transform.rotation.w } 
        };

      Vector3 angularVelocity = auv.GetComponent<Rigidbody>().angularVelocity;
      Vector3 linearVelocity = auv.GetComponent<Rigidbody>().linearVelocity;
      msg.twist = new Twist
      {
          linear = new Vector3 { x = linearVelocity.x, y = linearVelocity.y, z = linearVelocity.z },
          angular = new Vector3 { x = angularVelocity.x, y = angularVelocity.y, z = angularVelocity.z }
      };
      roscon.Publish(stateTopicName, msg);
    }
}
