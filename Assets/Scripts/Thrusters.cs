using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public class Thrusters : MonoBehaviour {
    ROSConnection roscon;
    public Rigidbody auvRb;
    public Transform[] thrusters;
    public ParticleSystem[] thrusterParticles;
    public float AUVRealForceMultiplier = 3;
    public string thrusterForcesTopicName = "/propulsion/forces";

    private double[] current_thruster_forces = new double[8];    
    private float massScalarRealToSim;

    void thrusterForceCallback(RosMessageTypes.Auv.ThrusterForcesMsg msg) {
        current_thruster_forces[0] = msg.SURGE_PORT;
        current_thruster_forces[1] = msg.SURGE_STAR;
        current_thruster_forces[2] = msg.SWAY_BOW;
        current_thruster_forces[3] = msg.SWAY_STERN;
        current_thruster_forces[4] = msg.HEAVE_BOW_PORT;
        current_thruster_forces[5] = msg.HEAVE_BOW_STAR;
        current_thruster_forces[6] = msg.HEAVE_STERN_STAR;
        current_thruster_forces[7] = msg.HEAVE_STERN_PORT;

        for (int i = 0; i < thrusters.Length; i++) {
            if (current_thruster_forces[i] > 0) {
                thrusterParticles[i].Play();
            } else {
                thrusterParticles[i].Stop();
                thrusterParticles[i].Clear();
            }
        }
    }

    // Start is called before the first frame update
    void Start() {
        massScalarRealToSim = 1f / AUVRealForceMultiplier;
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<RosMessageTypes.Auv.ThrusterForcesMsg>(thrusterForcesTopicName, thrusterForceCallback);
    }

    // Update is called once per frame
    void FixedUpdate() {
        for (int i = 0; i < thrusters.Length; i++) {
            if (thrusters[i].position.y < 0) {
                Vector3 worldForceDirection = thrusters[i].TransformDirection(Vector3.up);
                Vector3 force_in_direction = new Vector3(
                    worldForceDirection.x * (float)current_thruster_forces[i] * massScalarRealToSim,
                    worldForceDirection.y * (float)current_thruster_forces[i] * massScalarRealToSim,
                    worldForceDirection.z * (float)current_thruster_forces[i] * massScalarRealToSim
                );
                auvRb.AddForceAtPosition(force_in_direction, thrusters[i].position, ForceMode.Force);
            }
        }
    }
}
