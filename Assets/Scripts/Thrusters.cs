using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public class Thrusters : MonoBehaviour
{
	public float sinkForce = 30f;
	public float floatForce = 12f;
	public float moveForce = 15f;
	public float rotationForce = 1.0f;
	public Transform[] thrusters;
	public ParticleSystem[] thrusterParticles;
	public float AUVRealForceMultiplier = 3;
	public string thrusterForcesTopicName = "/propulsion/forces";

	private ROSConnection roscon;

	private bool isFrozen = false;
	private Rigidbody auvRb;
	private double[] rosThrusterForces = new double[8];
	private double[] inputThrusterForces = new double[8];
	private float massScalarRealToSim;
	
	// Pre-calculated force multipliers
	private float moveForceOver4, moveForceOver2, sinkForceOver4, floatForceOver4, rotationForceOver4;
	

	private void Start()
	{
		roscon = ROSConnection.GetOrCreateInstance();
		auvRb = GetComponent<Rigidbody>();
		roscon.Subscribe<RosMessageTypes.Auv.ThrusterForcesMsg>(thrusterForcesTopicName, SetThrusterForces);
		
		// Pre-calculate force multipliers as they are the same every frame
		massScalarRealToSim = 1f / AUVRealForceMultiplier;
		moveForceOver4 = moveForce / 4;
		moveForceOver2 = moveForce / 2;
		sinkForceOver4 = sinkForce / 4;
		floatForceOver4 = floatForce / 4;
		rotationForceOver4 = rotationForce / 4;
	}

	private void Update()
	{
		HandleFreezeInput();
		HandleMovementInput();
	}
	
	private void FixedUpdate()
	{
		for (int i = 0; i < thrusters.Length; i++)
		{
			// Don't calculate forces for thrusters above/out of the water
			if (thrusters[i].position.y >= 0) continue;
			
			// Calculate the force vector for each thruster
			float thrusterForceMagnitude = (float)(rosThrusterForces[i] + inputThrusterForces[i]);
			Vector3 worldForceDirection = thrusters[i].TransformDirection(Vector3.up);
			Vector3 thrusterForceVector = worldForceDirection * (thrusterForceMagnitude * massScalarRealToSim);
			
			// Apply the force to the AUV, at the thruster's position
			auvRb.AddForceAtPosition(thrusterForceVector, thrusters[i].position, ForceMode.Force);
			
			// Play particles if force is positive (i.e. forward thrust) and quality settings are high enough
			if (Math.Abs(thrusterForceMagnitude) > 0 && QualitySettings.GetQualityLevel() < 2)
			{
				thrusterParticles[i].Play();
			}
			else
			{
				thrusterParticles[i].Stop();
			}
		}
	}

	private void HandleFreezeInput()
	{
		if (Input.GetKeyDown(PlayerPrefs.GetString("freezeKeybind", "space")))
		{
			isFrozen = !isFrozen;
			auvRb.isKinematic = isFrozen;
		}
	}

	private void HandleMovementInput()
	{
		if (isFrozen) return;
		
		// Reset input forces to zero before recalculating them
		for (int i = 0; i < inputThrusterForces.Length; i++) inputThrusterForces[i] = 0;
		
		// No inputs => no thruster forces to add, so avoid checking each input
		if (!Input.anyKey) return;
		
		// Update input forces for each thruster based on key presses to control the AUV orientation and position
		if (Input.GetKey(PlayerPrefs.GetString("pitchKeybind", "i")))
		{
			inputThrusterForces[5] += rotationForceOver4;
			inputThrusterForces[2] += rotationForceOver4;
			inputThrusterForces[1] -= rotationForceOver4;
			inputThrusterForces[6] -= rotationForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("yawKeybind", "j")))
		{
			inputThrusterForces[4] += rotationForceOver4;
			inputThrusterForces[3] -= rotationForceOver4;
			inputThrusterForces[7] -= rotationForceOver4;
			inputThrusterForces[0] += rotationForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("negPitchKeybind", "k")))
		{
			inputThrusterForces[5] -= rotationForceOver4;
			inputThrusterForces[2] -= rotationForceOver4;
			inputThrusterForces[1] += rotationForceOver4;
			inputThrusterForces[6] += rotationForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("negYawKeybind", "l")))
		{
			inputThrusterForces[4] -= rotationForceOver4;
			inputThrusterForces[3] += rotationForceOver4;
			inputThrusterForces[7] += rotationForceOver4;
			inputThrusterForces[0] -= rotationForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("negRollKeybind", "u")))
		{
			inputThrusterForces[5] += rotationForceOver4;
			inputThrusterForces[2] -= rotationForceOver4;
			inputThrusterForces[6] += rotationForceOver4;
			inputThrusterForces[1] -= rotationForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("rollKeybind", "o")))
		{
			inputThrusterForces[5] -= rotationForceOver4;
			inputThrusterForces[2] += rotationForceOver4;
			inputThrusterForces[6] -= rotationForceOver4;
			inputThrusterForces[1] += rotationForceOver4;
		}
		// Control position.
		if (Input.GetKey(PlayerPrefs.GetString("surgeKeybind", "w")))
		{
			inputThrusterForces[4] -= moveForceOver4;
			inputThrusterForces[3] -= moveForceOver4;
			inputThrusterForces[7] += moveForceOver4;
			inputThrusterForces[0] += moveForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("swayKeybind", "a")))
		{
			inputThrusterForces[4] += moveForceOver2;
			inputThrusterForces[3] -= moveForceOver2;
			inputThrusterForces[7] += moveForceOver2;
			inputThrusterForces[0] -= moveForceOver2;
		}
		if (Input.GetKey(PlayerPrefs.GetString("negSurgeKeybind", "s")))
		{
			inputThrusterForces[4] += moveForceOver4;
			inputThrusterForces[3] += moveForceOver4;
			inputThrusterForces[7] -= moveForceOver4;
			inputThrusterForces[0] -= moveForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("negSwayKeybind", "d")))
		{
			inputThrusterForces[4] -= moveForceOver2;
			inputThrusterForces[3] += moveForceOver2;
			inputThrusterForces[7] -= moveForceOver2;
			inputThrusterForces[0] += moveForceOver2;
		}
		if (Input.GetKey(PlayerPrefs.GetString("negHeaveKeybind", "q")))
		{
			inputThrusterForces[5] += sinkForceOver4;
			inputThrusterForces[2] += sinkForceOver4;
			inputThrusterForces[6] += sinkForceOver4;
			inputThrusterForces[1] += sinkForceOver4;
		}
		if (Input.GetKey(PlayerPrefs.GetString("heaveKeybind", "e")))
		{
			inputThrusterForces[5] -= floatForceOver4;
			inputThrusterForces[2] -= floatForceOver4;
			inputThrusterForces[6] -= floatForceOver4;
			inputThrusterForces[1] -= floatForceOver4;
		}
	}
	
	private void SetThrusterForces(RosMessageTypes.Auv.ThrusterForcesMsg msg)
	{
		rosThrusterForces[0] = msg.FRONT_LEFT;
		rosThrusterForces[1] = msg.FRONT_RIGHT;
		rosThrusterForces[2] = msg.BACK_LEFT;
		rosThrusterForces[3] = msg.BACK_RIGHT;
		rosThrusterForces[4] = msg.HEAVE_FRONT_LEFT;
		rosThrusterForces[5] = msg.HEAVE_FRONT_RIGHT;
		rosThrusterForces[6] = msg.HEAVE_BACK_LEFT;
		rosThrusterForces[7] = msg.HEAVE_BACK_RIGHT;
	}
}
