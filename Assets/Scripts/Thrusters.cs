using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public class Thrusters : MonoBehaviour {
    ROSConnection roscon;
    public float sinkForce = 30f;
    public float floatForce = 12f;
    public float moveForce = 15f;
    public float rotationForce = 1.0f;
    public Transform[] thrusters;
    public ParticleSystem[] thrusterParticles;
    public float AUVRealForceMultiplier = 3;
    public string thrusterForcesTopicName = "/propulsion/forces";

    private bool isFrozen = false;
    private Rigidbody auvRb;
    private double[] current_ros_thruster_forces = new double[8];    
    private double[] current_keyboard_thruster_forces = new double[8];    
    private float massScalarRealToSim;

    void thrusterForceCallback(RosMessageTypes.Auv.ThrusterForcesMsg msg) {
        current_ros_thruster_forces[4] = msg.FRONT_LEFT;
        current_ros_thruster_forces[3] = msg.FRONT_RIGHT;
        current_ros_thruster_forces[7] = msg.BACK_LEFT;
        current_ros_thruster_forces[0] = msg.BACK_RIGHT;
        current_ros_thruster_forces[5] = msg.HEAVE_FRONT_LEFT;
        current_ros_thruster_forces[2] = msg.HEAVE_FRONT_RIGHT;
        current_ros_thruster_forces[6] = msg.HEAVE_BACK_LEFT;
        current_ros_thruster_forces[1] = msg.HEAVE_BACK_RIGHT;
    }
    
    // Start is called before the first frame update
    void Start() {
        massScalarRealToSim = 1f / AUVRealForceMultiplier;
        roscon = ROSConnection.GetOrCreateInstance();
        auvRb = GetComponent<Rigidbody>();
        roscon.Subscribe<RosMessageTypes.Auv.ThrusterForcesMsg>(thrusterForcesTopicName, thrusterForceCallback);
    }

    void Update()
    {
        HandleFreezeInput();
        HandleMovementInput();
    }

    void HandleFreezeInput()
    {
        if (Input.GetKeyDown(PlayerPrefs.GetString("freezeKeybind", "space")))
        {
            isFrozen = !isFrozen;

            if (isFrozen)
            {
                auvRb.isKinematic = true;
            } else {
                auvRb.isKinematic = false;
            }
        }
    }
    
    void HandleMovementInput()
    {
        if (!isFrozen)
        {
            current_keyboard_thruster_forces[4] = 0;
            current_keyboard_thruster_forces[3] = 0;
            current_keyboard_thruster_forces[7] = 0;
            current_keyboard_thruster_forces[0] = 0;
            current_keyboard_thruster_forces[5] = 0;
            current_keyboard_thruster_forces[2] = 0;
            current_keyboard_thruster_forces[6] = 0;
            current_keyboard_thruster_forces[1] = 0;
            // control orientation
            if (Input.GetKey(PlayerPrefs.GetString("pitchKeybind", "i"))) {
                current_keyboard_thruster_forces[5] += rotationForce / 4;
                current_keyboard_thruster_forces[2] += rotationForce / 4;
                current_keyboard_thruster_forces[6] -= rotationForce / 4;
                current_keyboard_thruster_forces[1] -= rotationForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("yawKeybind", "j"))) {
                current_keyboard_thruster_forces[4] -= rotationForce / 4;
                current_keyboard_thruster_forces[3] += rotationForce / 4;
                current_keyboard_thruster_forces[7] += rotationForce / 4;
                current_keyboard_thruster_forces[0] -= rotationForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("negPitchKeybind", "k"))) {
                current_keyboard_thruster_forces[5] -= rotationForce / 4;
                current_keyboard_thruster_forces[2] -= rotationForce / 4;
                current_keyboard_thruster_forces[6] += rotationForce / 4;
                current_keyboard_thruster_forces[1] += rotationForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("negYawKeybind", "l"))) {
                current_keyboard_thruster_forces[4] += rotationForce / 4;
                current_keyboard_thruster_forces[3] -= rotationForce / 4;
                current_keyboard_thruster_forces[7] -= rotationForce / 4;
                current_keyboard_thruster_forces[0] += rotationForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("negRollKeybind", "u"))) {
                current_keyboard_thruster_forces[5] += rotationForce / 4;
                current_keyboard_thruster_forces[2] -= rotationForce / 4;
                current_keyboard_thruster_forces[6] += rotationForce / 4;
                current_keyboard_thruster_forces[1] -= rotationForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("rollKeybind", "o"))) {
                current_keyboard_thruster_forces[5] -= rotationForce / 4;
                current_keyboard_thruster_forces[2] += rotationForce / 4;
                current_keyboard_thruster_forces[6] -= rotationForce / 4;
                current_keyboard_thruster_forces[1] += rotationForce / 4;
            }
            //control position
            if (Input.GetKey(PlayerPrefs.GetString("surgeKeybind", "w"))) {
                current_keyboard_thruster_forces[4] += moveForce / 4;
                current_keyboard_thruster_forces[3] += moveForce / 4;
                current_keyboard_thruster_forces[7] -= moveForce / 4;
                current_keyboard_thruster_forces[0] -= moveForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("swayKeybind", "a"))) {
                current_keyboard_thruster_forces[4] -= moveForce / 2;
                current_keyboard_thruster_forces[3] += moveForce / 2;
                current_keyboard_thruster_forces[7] -= moveForce / 2;
                current_keyboard_thruster_forces[0] += moveForce / 2;
            }
            if (Input.GetKey(PlayerPrefs.GetString("negSurgeKeybind", "s"))) {
                current_keyboard_thruster_forces[4] -= moveForce / 4;
                current_keyboard_thruster_forces[3] -= moveForce / 4;
                current_keyboard_thruster_forces[7] += moveForce / 4;
                current_keyboard_thruster_forces[0] += moveForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("negSwayKeybind", "d"))) {
                current_keyboard_thruster_forces[4] += moveForce / 2;
                current_keyboard_thruster_forces[3] -= moveForce / 2;
                current_keyboard_thruster_forces[7] += moveForce / 2;
                current_keyboard_thruster_forces[0] -= moveForce / 2;
            }
            if (Input.GetKey(PlayerPrefs.GetString("negHeaveKeybind", "q"))) {
                current_keyboard_thruster_forces[5] += sinkForce / 4;
                current_keyboard_thruster_forces[2] += sinkForce / 4;
                current_keyboard_thruster_forces[6] += sinkForce / 4;
                current_keyboard_thruster_forces[1] += sinkForce / 4;
            }
            if (Input.GetKey(PlayerPrefs.GetString("heaveKeybind", "e"))) {
                current_keyboard_thruster_forces[5] -= floatForce / 4;
                current_keyboard_thruster_forces[2] -= floatForce / 4;
                current_keyboard_thruster_forces[6] -= floatForce / 4;
                current_keyboard_thruster_forces[1] -= floatForce / 4;
            }
            
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        for (int i = 0; i < thrusters.Length; i++) {
            if (thrusters[i].position.y < 0) {
                double current_thruster_force = current_ros_thruster_forces[i] + current_keyboard_thruster_forces[i];
                if (Math.Abs(current_thruster_force) > 0 && QualitySettings.GetQualityLevel() < 2) {
                    thrusterParticles[i].Play();
                } else {
                    thrusterParticles[i].Stop();
                }
                Vector3 worldForceDirection = thrusters[i].TransformDirection(Vector3.down);
                Vector3 force_in_direction = new Vector3(
                    worldForceDirection.x * (float)current_thruster_force * massScalarRealToSim,
                    worldForceDirection.y * (float)current_thruster_force * massScalarRealToSim,
                    worldForceDirection.z * (float)current_thruster_force * massScalarRealToSim
                );
                auvRb.AddForceAtPosition(force_in_direction, thrusters[i].position, ForceMode.Force);
            }
        }
    }
}
