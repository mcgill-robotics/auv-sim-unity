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
    public int updateFrequency = 10;

    public GameObject auv;

    private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();
    private float timeSinceLastUpdate;

    // Start is called before the first frame update
    void Start() {
      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName); 
    }

    // Update is called once per frame
    void Update() {
      timeSinceLastUpdate += Time.deltaTime;
      if (timeSinceLastUpdate < 1.0/updateFrequency) {
        return;
      }
      timeSinceLastUpdate = 0;

      msg.position = auv.transform.position.To<RUF>();

      Quaternion rotation = auv.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
      msg.orientation = rotation.To<NED>();
      msg.angular_velocity = auv.GetComponent<Rigidbody>().angularVelocity.To<RUF>();
      msg.velocity = auv.GetComponent<Rigidbody>().velocity.To<RUF>();

      msg.isDVLActive = isDVLActive;
      msg.isDepthSensorActive = isDepthSensorActive;
      msg.isIMUActive = isIMUActive;

      roscon.Publish(stateTopicName, msg);
    }
}
