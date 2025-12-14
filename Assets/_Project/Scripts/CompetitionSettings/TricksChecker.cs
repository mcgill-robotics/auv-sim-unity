using UnityEngine;
using System;

/// <summary>
/// Competition task: Awards points for 360-degree rotations (roll, pitch, yaw) near a target prop.
/// </summary>
public class TricksChecker : MonoBehaviour, ICompetitionTask
{
    public static TricksChecker instance;
    public Transform auv;
    public Transform targetProp;
    public int pointYawAvailable;
    public int pointPitchRollAvailable;

    private bool isInitialRotation;
    private Quaternion initialRotation;
    private Quaternion lastRotation;
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
        this.enabled = false;
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

        Quaternion curRotation = auv.rotation;

        if (isInitialRotation)
        {
            initialRotation = curRotation;
            lastRotation = curRotation;
            isInitialRotation = false;
        }

        Quaternion deltaRotation = Quaternion.Inverse(lastRotation) * curRotation;

        Vector3 deltaEuler = deltaRotation.eulerAngles;

        float deltaRoll = Mathf.DeltaAngle(0, deltaEuler.x);
        float deltaPitch = Mathf.DeltaAngle(0, deltaEuler.z);
        float deltaYaw = Mathf.DeltaAngle(0, deltaEuler.y);

        rollTotal += deltaRoll;
        pitchTotal += deltaPitch;
        yawTotal += deltaYaw;

        // Check for full 360-degree rotations.
        if (Mathf.Abs(rollTotal) >= 360f)
        {
            rollCount++;
            rollTotal = 0;
            PointsManager.instance.AddPoint(pointPitchRollAvailable, "Gate");
            MessageBox.instance.AddMessage(string.Format("Tricks Roll +{0}pts", pointPitchRollAvailable));
        }
        if (Mathf.Abs(pitchTotal) >= 360f)
        {
            pitchCount++;
            pitchTotal = 0;
            PointsManager.instance.AddPoint(pointPitchRollAvailable, "Gate");
            MessageBox.instance.AddMessage(string.Format("Tricks Pitch +{0}pts", pointPitchRollAvailable));
        }
        if (Mathf.Abs(yawTotal) >= 360f)
        {
            yawCount++;
            yawTotal = 0;
            PointsManager.instance.AddPoint(pointYawAvailable, "Gate");
            MessageBox.instance.AddMessage(string.Format("Tricks Yaw +{0}pts", pointYawAvailable));
        }

        lastRotation = curRotation;
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