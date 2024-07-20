using UnityEngine;
using System;

public class TricksChecker : MonoBehaviour
{
	public static TricksChecker instance;
	public Transform auv;
	public Transform targetProp;
	public int pointYawAvailable;
	public int pointPitchRollAvailable;

	private bool isInitialRotation;
	private Vector3 initialRotation;
	private Vector3 lastRotation;
	private float maxDistance = 3f;
	private int tricksAllowed = 2;
	private int rollCount = 0;
	private int pitchCount = 0;
	private int yawCount = 0;
	private float rollTotal = 0f;
	private float pitchTotal = 0f;
	private float yawTotal = 0f;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		this.enabled = enabled;
	}

	void Update()
	{
		if (yawCount + rollCount + pitchCount >= tricksAllowed)
		{
			StopScript();
		}

		float distance = Vector3.Distance(targetProp.position, auv.position);
		// Reset tricks count.
		if (distance > maxDistance)
		{
			isInitialRotation = true;
			rollCount = 0;
			pitchCount = 0;
			yawCount = 0;
			return;
		}

		Vector3 curRotation = auv.eulerAngles;

		if (isInitialRotation)
		{
			initialRotation = curRotation;
			lastRotation = curRotation;
			isInitialRotation = false;
		}

		float deltaRoll = CalculateDelta(curRotation.x, lastRotation.x);
		float deltaPitch = CalculateDelta(curRotation.z, lastRotation.z);
		float deltaYaw = CalculateDelta(curRotation.y, lastRotation.y);
		Debug.Log(string.Format("deltaRoll = {0} | curRotation = {1} | lastRoration = {2}", deltaRoll, curRotation.x, lastRotation.x));
		// Debug.Log(string.Format("rollTotal = {0} | curRotation = {1} | lastRoration = {2}", rollTotal, curRotation.x, lastRotation.x));

		// Accumulate rotation.
		rollTotal += deltaRoll;
		pitchTotal += deltaPitch;
		yawTotal += deltaYaw;

		// Check for full 360 degree rotations.
		if (Mathf.Abs(rollTotal) >= 360f)
		{
			rollCount++;
			rollTotal = 0;
			Debug.Log("ROLL COMPLETED");
			PointsManager.instance.AddPoint(pointPitchRollAvailable, "Gate");
		}
		if (Mathf.Abs(pitchTotal) >= 360f)
		{
			pitchCount++;
			pitchTotal = 0;
			Debug.Log("PITCH COMPLETED");
			PointsManager.instance.AddPoint(pointPitchRollAvailable, "Gate");
		}
		if (Mathf.Abs(yawTotal) >= 360f)
		{
			yawCount++;
			yawTotal = 0;
			Debug.Log("YAW COMPLETED");
			PointsManager.instance.AddPoint(pointYawAvailable, "Gate");
		}

		lastRotation = curRotation;
	}

	private float CalculateDelta(float currentAngle, float lastAngle)
	{
		if (lastAngle - currentAngle > 180)
		{
			return 360 - lastAngle - currentAngle;
		}
		else if (lastAngle - currentAngle < -180)
		{
			return -(360 - lastAngle - currentAngle);
		}
		return currentAngle - lastAngle;
	}

	public void StartScript()
	{
		this.enabled = true; // Enable the script.
	}

	public void StopScript()
	{
		rollCount = 0;
		pitchCount = 0;
		yawCount = 0;
		this.enabled = false; // Disable the script.
	}
}