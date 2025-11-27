using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Auv;

/// <summary>
/// Manages all pinger bearing visualizations.
/// Shows both true (ground truth) and expected (algorithm output) bearings.
/// </summary>
public class PingerBearingVisualizer : MonoBehaviour
{
    [Header("AUV Reference")]
    public Transform Diana;

    [Header("Pinger References")]
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;

    [Header("True Bearing Arrows (Ground Truth)")]
    public Transform trueBearing1;
    public Transform trueBearing2;
    public Transform trueBearing3;
    public Transform trueBearing4;

    [Header("Expected Bearing Arrows (ROS Algorithm)")]
    public Transform expectedBearing1;
    public Transform expectedBearing2;
    public Transform expectedBearing3;
    public Transform expectedBearing4;

    [Header("Dependencies")]
    [SerializeField] private PingerTimeDifference pingerTimeDifference;

    private Transform[] pingers = new Transform[4];
    private Transform[] trueBearings = new Transform[4];
    private Transform[] expectedBearings = new Transform[4];
    private int[] frequencies = new int[4];
    private ROSConnection roscon;
    private Quaternion defaultRotation = new Quaternion(1.0f, 0f, 0f, 0.0f);

    void Start()
    {
        InitializeArrays();

        if (pingerTimeDifference != null)
        {
            frequencies = pingerTimeDifference.frequencies;
        }
        else
        {
            Debug.LogWarning("[PingerBearingVisualizer] PingerTimeDifference not assigned. Expected bearings will not work.");
        }

        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(ROSSettings.Instance.PingerBearingTopic, OnPingerBearingReceived);
    }

    private void InitializeArrays()
    {
        pingers[0] = pinger1;
        pingers[1] = pinger2;
        pingers[2] = pinger3;
        pingers[3] = pinger4;

        trueBearings[0] = trueBearing1;
        trueBearings[1] = trueBearing2;
        trueBearings[2] = trueBearing3;
        trueBearings[3] = trueBearing4;

        expectedBearings[0] = expectedBearing1;
        expectedBearings[1] = expectedBearing2;
        expectedBearings[2] = expectedBearing3;
        expectedBearings[3] = expectedBearing4;
    }

    void Update()
    {
        UpdateTrueBearings();
    }

    /// <summary>
    /// Updates true bearings every frame based on actual pinger positions.
    /// </summary>
    private void UpdateTrueBearings()
    {
        if (Diana == null) return;

        for (int i = 0; i < pingers.Length; i++)
        {
            if (pingers[i] != null && trueBearings[i] != null)
            {
                Vector3 direction = pingers[i].position - Diana.position;
                SetBearing(trueBearings[i], direction);
            }
        }
    }

    /// <summary>
    /// Callback for ROS pinger bearing messages.
    /// Updates expected bearings based on algorithm output.
    /// </summary>
    private void OnPingerBearingReceived(PingerBearingMsg msg)
    {
        int frequencyIndex = Array.IndexOf(frequencies, msg.frequency);
        
        if (frequencyIndex < 0)
        {
            Debug.LogWarning($"[PingerBearingVisualizer] Unknown frequency: {msg.frequency}");
            return;
        }

        // Convert ROS coordinates (NED) to Unity coordinates
        // ROS: x=north, y=east, z=down
        // Unity: x=right, y=up, z=forward
        Vector3 bearingDirection = new Vector3(
            -(float)msg.pinger_bearing.y,
            (float)msg.pinger_bearing.z,
            (float)msg.pinger_bearing.x
        );

        SetBearing(expectedBearings[frequencyIndex], bearingDirection);
    }

    /// <summary>
    /// Sets a bearing arrow to point in a specific direction.
    /// </summary>
    private void SetBearing(Transform bearing, Vector3 direction)
    {
        if (bearing == null || Diana == null) return;

        // Position slightly above AUV
        bearing.position = Diana.position + new Vector3(0, 1, 0);

        // Zero out Y for horizontal bearing
        direction.y = 0;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            bearing.rotation = Quaternion.LookRotation(direction) * defaultRotation;
        }
    }
}
