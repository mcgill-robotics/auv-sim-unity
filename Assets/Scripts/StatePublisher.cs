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
    public Transform pinger3;
    public Transform pinger4;

    private RosMessageTypes.Auv.UnityStateMsg msg = new RosMessageTypes.Auv.UnityStateMsg();
    private float timeSinceLastUpdate;
    bool isDVLActive = true;
    bool isDepthSensorActive = true;
    bool isIMUActive = true;
    bool isHydrophonesActive = true;
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

      double speedOfSound = 1480.0;

      // Assumption: H1 is the origin hydrophone
      double d1 = Vector3.Distance(hydrophone1.position, pinger1.position);
      double d2 = Vector3.Distance(hydrophone2.position, pinger1.position);
      double d3 = Vector3.Distance(hydrophone3.position, pinger1.position);
      // Debug.Log("D1: " + d1 + " D2: " + d2 + " D3: " + d3);

      double time1 = d1 / speedOfSound;
      double time2 = d2 / speedOfSound;
      double time3 = d3 / speedOfSound;
      // Debug.Log("Time1: " + time1 + " Time2: " + time2 + " Time3: " + time3);

      double time2Diff = time2 - time1;
      double time3Diff = time3 - time1;

      double[] dt_pinger1 = {time2Diff, time3Diff};

      // Debug.Log("Time2Diff: " + time2Diff + " Time3Diff: " + time3Diff);
      msg.dt_pinger1 = dt_pinger1;

      roscon.Publish(stateTopicName, msg);
    }
}
