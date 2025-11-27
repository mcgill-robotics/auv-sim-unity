using UnityEngine;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System;

public class StatePublisher : ROSPublisher
{
    protected override string Topic => ROSSettings.Instance.StateTopic;

    public GameObject auv;
    public Rigidbody auvRb;

    [SerializeField] private PingerTimeDifference pingerTimeDifference;
    private UnityStateMsg stateMsg;
    private Vector3 lastVelocity;
    private int numberOfPingers = 4;

    protected override void Start()
    {
        base.Start();
        
        if (pingerTimeDifference == null)
        {
            Debug.LogWarning("[StatePublisher] PingerTimeDifference not assigned. Pinger data will not be published.");
        }
        
        stateMsg = new UnityStateMsg();
        lastVelocity = auvRb.linearVelocity;
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<UnityStateMsg>(Topic);
    }

    protected override void PublishMessage()
    {
        Vector3 currentVelocity = auvRb.linearVelocity;
        Vector3 acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currentVelocity;

        stateMsg.position = auv.transform.position.To<RUF>();
        stateMsg.position.y *= -1; // Convert to depth

        Quaternion rotation = auv.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
        stateMsg.orientation = rotation.To<NED>();
        stateMsg.angular_velocity = auvRb.angularVelocity.To<RUF>();
        stateMsg.velocity = currentVelocity.To<RUF>();
        stateMsg.linear_acceleration = acceleration.To<RUF>();

        // Update sensor status from SimulationSettings
        stateMsg.isDVLActive = Convert.ToInt32(SimulationSettings.Instance.PublishDVL);
        stateMsg.isDepthSensorActive = Convert.ToInt32(SimulationSettings.Instance.PublishDepth);
        stateMsg.isIMUActive = Convert.ToInt32(SimulationSettings.Instance.PublishIMU);
        stateMsg.isHydrophonesActive = Convert.ToInt32(SimulationSettings.Instance.PublishHydrophones);

        // Pinger data
        if (pingerTimeDifference != null)
        {
            int[] frequencies = new int[numberOfPingers];
            uint[][] times = new uint[numberOfPingers][];
            for (int i = 0; i < numberOfPingers; i++)
            {
                (times[i], frequencies[i]) = pingerTimeDifference.CalculateTimeDifference(i);
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