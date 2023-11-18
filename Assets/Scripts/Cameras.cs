using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

/* TO-DO */

public class Cameras : MonoBehaviour {
    ROSConnection roscon;

    public string pubTopicName = "/SOME_NAME";

    // Start is called before the first frame update
    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<>(pubTopicName);
    }

    // Update is called once per frame
    void Update() {
        
    }
}