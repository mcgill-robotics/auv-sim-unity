using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public class thrusters : MonoBehaviour {
    ROSConnection roscon;

    public string subTopicName = "/thruster_report";

    double[] forces = new double[8];    

    void thrusterCallback(RosMessageTypes.Auv.ThrusterReportMsg msg) {
        forces[0] = msg.SURGE_PORT;
        forces[1] = msg.SURGE_STAR;
        forces[2] = msg.SWAY_BOW;
        forces[3] = msg.SWAY_STERN;
        forces[4] = msg.HEAVE_BOW_PORT;
        forces[5] = msg.HEAVE_BOW_STAR;
        forces[8] = msg.HEAVE_STERN_STAR;
        forces[7] = msg.HEAVE_STERN_PORT;
    }

    // Start is called before the first frame update
    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<RosMessageTypes.Auv.ThrusterReportMsg>(subTopicName, thrusterCallback);
    }

    // Update is called once per frame
    void Update() {
        
    }
}
