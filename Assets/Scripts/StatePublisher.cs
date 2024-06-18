using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System;

public class StatePublisher : MonoBehaviour {
    private ROSConnection roscon;
    public string stateTopicName = "/unity/state";
    public GameObject auv;
    public Rigidbody auvRigidBody;
    Vector3 acceleration;
    Vector3 lastVelocity;
    Vector3 currentVelocity;
    private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();
    private float timeSinceLastUpdate;
    int isDVLActive = 1;
    int isDepthSensorActive = 1;
    int isIMUActive = 1;
    int isHydrophonesActive = 1; 
    bool publishToRos = true;

    int updateFrequency = 10;

    // Start is called before the first frame update
    void Start() {
      lastVelocity = auvRigidBody.velocity;
      acceleration = new Vector3(0, 0, 0);

      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName); 
    }

    // Update is called once per frame
    void Update() {
      publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
      updateFrequency = int.Parse(PlayerPrefs.GetString("poseRate", "50"));
      isDVLActive = Convert.ToInt32(bool.Parse(PlayerPrefs.GetString("PublishDVLToggle", "true")));
      isDepthSensorActive = Convert.ToInt32(bool.Parse(PlayerPrefs.GetString("PublishDepthToggle", "true")));
      isIMUActive = Convert.ToInt32(bool.Parse(PlayerPrefs.GetString("PublishIMUToggle", "true")));
      isHydrophonesActive = Convert.ToInt32(bool.Parse(PlayerPrefs.GetString("PublishHydrophonesToggle", "true")));

      timeSinceLastUpdate += Time.deltaTime;
      if (timeSinceLastUpdate < 1.0/updateFrequency || !publishToRos) {
        return;
      }
      timeSinceLastUpdate = 0;

      currentVelocity = auvRigidBody.velocity;
      acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
      lastVelocity = currentVelocity;

      msg.position = auv.transform.position.To<RUF>();

      Quaternion rotation = auv.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
      msg.orientation = rotation.To<NED>();
      msg.angular_velocity = auv.GetComponent<Rigidbody>().angularVelocity.To<RUF>();
      msg.velocity = auv.GetComponent<Rigidbody>().velocity.To<RUF>();
      msg.linear_acceleration = acceleration.To<RUF>();

      msg.isDVLActive = isDVLActive;
      msg.isDepthSensorActive = isDepthSensorActive;
      msg.isIMUActive = isIMUActive;
      msg.isHydrophonesActive = isHydrophonesActive;

      roscon.Publish(stateTopicName, msg);
    }
}
