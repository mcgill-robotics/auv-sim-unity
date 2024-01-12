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
    public Transform hydrophone1;
    public Transform hydrophone2;
    public Transform hydrophone3;
    public Transform pinger1;
    public Transform pinger2;

    private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();
    private float timeSinceLastUpdate;
    bool isDVLActive = true;
    bool isDepthSensorActive = true;
    bool isIMUActive = true;
    bool publishToRos = true;
    int updateFrequency = 10;

    // Start is called before the first frame update
    void Start() {
      roscon = ROSConnection.GetOrCreateInstance();
      roscon.RegisterPublisher<RosMessageTypes.Auv.UnityStateMsg>(stateTopicName); 
    }

    // Update is called once per frame
    void Update() {
      publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"));
      updateFrequency = int.Parse(PlayerPrefs.GetString("poseRate", "10"));
      isDVLActive = bool.Parse(PlayerPrefs.GetString("PublishDVLToggle", "true"));
      isDepthSensorActive = bool.Parse(PlayerPrefs.GetString("PublishDepthToggle", "true"));
      isIMUActive = bool.Parse(PlayerPrefs.GetString("PublishIMUToggle", "true"));

      timeSinceLastUpdate += Time.deltaTime;
      if (timeSinceLastUpdate < 1.0/updateFrequency || !publishToRos) {
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

      float speedOfSound = 1480.0f;

      // Assumption: H1 is the origin hydrophone ------------------- definetely wrong
      float d1 = Vector3.Distance(hydrophone1.position, pinger1.position);
      float d2 = Vector3.Distance(hydrophone2.position, pinger1.position);
      float d3 = Vector3.Distance(hydrophone3.position, pinger1.position);

      float time1 = Math.Abs(d1 / speedOfSound);
      float time2 = Math.Abs(d2 / speedOfSound);
      float time3 = Math.Abs(d3 / speedOfSound);

      float time1Diff = Math.Abs(time1 - time1);
      float time2Diff = Math.Abs(time2 - time1);
      float time3Diff = Math.Abs(time3 - time1);

      // msg.hydrophones_time_diff = new Vector3(time1Diff, time2Diff, time3Diff).To<RUF>();

      roscon.Publish(stateTopicName, msg);
    }
}
