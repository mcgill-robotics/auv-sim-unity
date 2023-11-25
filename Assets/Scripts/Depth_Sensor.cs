using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;

/* TO-DO */

public class DepthSensor : MonoBehaviour {
    ROSConnection roscon;
    
    [SerializeField]
    //publish delay in milliseconds
    // this should be set to the same as the depth sensor's update rate
    public float publishDelay = 10;

    private string pubTopicName = "/depth";
    public RosMessageTypes.Std.Float64Msg depth;

    private float lastPublishTime = 0;

    // Start is called before the first frame update
    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<Float64Msg>(pubTopicName);
    }

    // Update is called once per frame
    void Update() {
        if (Time.time - lastPublishTime < publishDelay / 1000) {
            return;
        }
        depth.data = transform.position.y;
        roscon.Publish(pubTopicName, depth);
        lastPublishTime = Time.time;
    }
}