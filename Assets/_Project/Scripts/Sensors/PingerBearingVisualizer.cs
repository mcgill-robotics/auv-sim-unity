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
    public Transform Douglas;

    [Header("Pingers (To visualize bearings)")]
    [Tooltip("Array of pingers transforms (4 max), used to visualize bearings on each hydrophone")]
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;


    [Header("Dependencies")]
    [SerializeField] private PingerTimeDifference pingerTimeDifference;

    private Transform[] pingers = new Transform[4];
    private Transform[] hydrophones = new Transform[3];
    private GameObject[] trueBearings;
    private GameObject[] expectedBearings;
    private int[] frequencies = new int[4];
    public Color[] visualizationBearingColors = new Color[] {Color.white, Color.black, Color.magenta, Color.grey};
    private Material expectedBearingArrowMat;
    private Material trueBearingArrowMat;
    private ROSConnection roscon;
    private Quaternion defaultRotation = new Quaternion(1.0f, 0f, 0f, 0.0f);

    void Start()
    {
        InitializeArraysAndArrows();

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

    private void InitializeArraysAndArrows()
    {
        // Initialize Arrays
        pingers[0] = pinger1;
        pingers[1] = pinger2;
        pingers[2] = pinger3;
        pingers[3] = pinger4;

        // Create arrows for expected bearings and true bearings
        expectedBearings = new GameObject[pingers.Length];
        trueBearings = new GameObject[pingers.Length];

        for (int i = 0; i < pingers.Length ; i++)
        {

            expectedBearingArrowMat = Utils.VisualizationUtils.CreateMaterial(visualizationBearingColors[i]);
            trueBearingArrowMat = Utils.VisualizationUtils.CreateMaterial(visualizationBearingColors[i]);

            GameObject templateExpectedBearingArrow = Utils.VisualizationUtils.CreateArrow("DefaultArrow", expectedBearingArrowMat, 0.2f);
            GameObject templateTrueBearingArrow = Utils.VisualizationUtils.CreateArrow("DefaultArrow", trueBearingArrowMat, 0.2f);

            expectedBearings[i] = Instantiate(templateExpectedBearingArrow, Douglas.position, Quaternion.identity);
            expectedBearings[i].transform.parent = Douglas; // Parent to AUV so it moves with AUV
            Utils.VisualizationUtils.SetXRayLayer(expectedBearings[i]);
            expectedBearings[i].transform.localScale = new Vector3(1f / 10f, 1f / 10f, 1f / 10f);
            expectedBearings[i].SetActive(false);

            trueBearings[i] = Instantiate(templateTrueBearingArrow, Douglas.position, Quaternion.identity);
            trueBearings[i].transform.parent = Douglas; // Parent to thruster so it moves with AUV
            Utils.VisualizationUtils.SetXRayLayer(trueBearings[i]);
            trueBearings[i].transform.localScale = new Vector3(1f / 10f, 1f / 10f, 1f / 10);
            trueBearings[i].SetActive(false);

            // Destroy the template (instances keep their own copies of the GO but share the material)
            Destroy(templateExpectedBearingArrow);
            Destroy(templateTrueBearingArrow);
        }

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
        if (Douglas == null) return;

        for (int i = 0; i < pingers.Length; i++)
        {
            if (pingers[i] != null && trueBearings[i] != null)
            {
                Vector3 direction = pingers[i].position - Douglas.position;
                trueBearings[i].SetActive(true);
                SetBearing(trueBearings[i].transform, direction);
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

        //SetBearing(expectedBearings[frequencyIndex], bearingDirection);
    }

    /// <summary>
    /// Sets a bearing arrow to point in a specific direction.
    /// </summary>
    private void SetBearing(Transform bearing, Vector3 direction)
    {
        if (bearing == null || Douglas == null) return;

        // Position at hydrophones
        bearing.position = Douglas.position + new Vector3(0, 0.3f, 0);

        // Zero out Y for horizontal bearing
        direction.y = 0.02f;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            bearing.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);;
        }
    }
}
