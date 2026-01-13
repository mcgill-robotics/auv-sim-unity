using UnityEngine;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System;

public class StatePublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.StateTopic;

    [Header("Physical Setup")]
    [Tooltip("AUV root Transform - leave empty to use SimulationSettings.AUVTransform")]
    [SerializeField] private Transform auvOverride;
    
    [Tooltip("AUV Rigidbody - leave empty to use SimulationSettings.AUVRigidbody")]
    [SerializeField] private Rigidbody auvRbOverride;
    
    /// <summary>Returns the AUV Transform from override or SimulationSettings.</summary>
    private Transform AuvTransform => auvOverride != null ? auvOverride : SimulationSettings.Instance?.AUVTransform;
    
    /// <summary>Returns the AUV Rigidbody from override or SimulationSettings.</summary>
    private Rigidbody AuvRb => auvRbOverride != null ? auvRbOverride : SimulationSettings.Instance?.AUVRigidbody;

    [Space(10)]
    [Header("Hydrophone System")]
    [Tooltip("Reference to pinger time difference calculator. Leave empty to disable hydrophone data publishing")]
    [SerializeField] private PingerTimeDifference pingerTimeDifference;
    private UnityStateMsg stateMsg;
    private Vector3 lastVelocity;
    private int numberOfPingers = 4;
    
    // Reusable arrays to avoid per-frame allocations
    private int[] frequencies;
    private uint[][] times;
    
    // Cached quaternion to avoid per-frame allocation
    private static readonly Quaternion rotationOffset = Quaternion.Euler(0f, 90f, 0f);

    protected override void Start()
    {
        base.Start();
        
        if (pingerTimeDifference == null)
        {
            Debug.LogWarning("[StatePublisher] PingerTimeDifference not assigned. Pinger data will not be published.");
        }
        
        stateMsg = new UnityStateMsg();
        lastVelocity = AuvRb.linearVelocity;
        
        // Initialize reusable arrays
        frequencies = new int[numberOfPingers];
        times = new uint[numberOfPingers][];
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<UnityStateMsg>(Topic);
    }

    public override void PublishMessage()
    {
        Vector3 currentVelocity = AuvRb.linearVelocity;
        Vector3 acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currentVelocity;

        stateMsg.position = AuvTransform.position.To<RUF>();
        stateMsg.position.y *= -1; // Convert to depth

        Quaternion rotation = AuvTransform.rotation * rotationOffset;
        stateMsg.orientation = rotation.To<NED>();
        stateMsg.angular_velocity = AuvRb.angularVelocity.To<RUF>();
        stateMsg.velocity = currentVelocity.To<RUF>();
        stateMsg.linear_acceleration = acceleration.To<RUF>();

        // Update sensor status from SimulationSettings
        stateMsg.is_dvl_active = Convert.ToInt32(SimulationSettings.Instance.PublishDVL);
        stateMsg.is_depth_sensor_active = Convert.ToInt32(SimulationSettings.Instance.PublishDepth);
        stateMsg.is_imu_active = Convert.ToInt32(SimulationSettings.Instance.PublishIMU);
        stateMsg.is_hydrophones_active = Convert.ToInt32(SimulationSettings.Instance.PublishHydrophones);

        // Pinger data
        if (pingerTimeDifference != null)
        {
            // Reuse existing arrays
            for (int i = 0; i < numberOfPingers; i++)
            {
                // Note: CalculateTimeDifference returns a reused array reference.
                // We MUST Clone it because 'times[i]' needs to store distinct data for this specific pinger,
                // and the next call to CalculateTimeDifference will overwrite the same buffer.
                var (t, f) = pingerTimeDifference.CalculateTimeDifference(i);
                times[i] = (uint[])t.Clone(); 
                frequencies[i] = f;
            }
            stateMsg.frequencies = frequencies;
            stateMsg.hydrophone_one_freqs = times[0];
            stateMsg.hydrophone_two_freqs = times[1];
            stateMsg.hydrophone_three_freqs = times[2];
            stateMsg.hydrophone_four_freqs = times[3];
        }

        ros.Publish(Topic, stateMsg);
    }
}