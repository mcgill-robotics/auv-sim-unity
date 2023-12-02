using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public class Thrusters : MonoBehaviour {
    ROSConnection roscon;
    public Rigidbody auvRb;
    public Transform[] thrusterPositions;
    public Vector3[] thrusterDirections;
    public float AUVRealMass = 25;
    public string thrusterForcesTopicName = "/propulsion/thruster_forces";

    private double[] current_thruster_forces = new double[8];    
    private float massScalarRealToSim;

    void thrusterForceCallback(RosMessageTypes.Auv.ThrusterForcesMsg msg) {
        current_thruster_forces[0] = msg.SURGE_PORT;
        current_thruster_forces[1] = msg.SURGE_STAR;
        current_thruster_forces[2] = msg.SWAY_BOW;
        current_thruster_forces[3] = msg.SWAY_STERN;
        current_thruster_forces[4] = msg.HEAVE_BOW_PORT;
        current_thruster_forces[5] = msg.HEAVE_BOW_STAR;
        current_thruster_forces[8] = msg.HEAVE_STERN_STAR;
        current_thruster_forces[7] = msg.HEAVE_STERN_PORT;
    }

    // Start is called before the first frame update
    void Start() {
        massScalarRealToSim = auvRb.mass / AUVRealMass;
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<RosMessageTypes.Auv.ThrusterForcesMsg>(thrusterForcesTopicName, thrusterForceCallback);
    }

    // Update is called once per frame
    void FixedUpdate() {
        for (int i = 0; i < thrusterPositions.Length; i++) {
            if (thrusterPositions[i].position.y < 0) {
                Vector3 worldForcePosition = thrusterPositions[i].TransformDirection(thrusterDirections[i]);
                Vector3 force_in_direction = new Vector3(
                    worldForcePosition.x * (float)current_thruster_forces[i] * massScalarRealToSim,
                    worldForcePosition.y * (float)current_thruster_forces[i] * massScalarRealToSim,
                    worldForcePosition.z * (float)current_thruster_forces[i] * massScalarRealToSim
                );
                auvRb.AddForceAtPosition(force_in_direction, thrusterPositions[i].position, ForceMode.Force);
            }
        }
    }
}
