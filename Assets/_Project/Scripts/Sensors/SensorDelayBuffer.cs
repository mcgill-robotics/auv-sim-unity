using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements a time-stamped Ring Buffer / Queue that records the AUV Rigidbody's state
/// during FixedUpdate() to simulate sensor latency across I2C/Serial buses, ROS 2 middleware, and TCP bridge.
/// 
/// Also implements Zero-Order Hold acoustic throttling for DVL translation measurements,
/// creating a staircase/stepped signal between acoustic pings while leaving IMU orientation/rate unthrottled.
/// </summary>
[DefaultExecutionOrder(-75)]
public class SensorDelayBuffer : MonoBehaviour
{
    [Header("Latency Buffer Settings")]
    [Tooltip("Sensor latency delay in seconds (e.g., 0.15 for 150ms delay)")]
    public float delayTime = 0.15f;

    [Header("DVL Acoustic Throttling (Zero-Order Hold)")]
    [Tooltip("Time interval between acoustic bottom-locks in seconds (e.g., 0.175s for ~5.7 Hz ping rate)")]
    [Range(0.1f, 0.3f)]
    public float dvlPingInterval = 0.175f;

    private Rigidbody rb;
    private List<RigidbodyStateSample> buffer = new List<RigidbodyStateSample>();

    private float dvlPingTimer = 0f;
    private Vector3 lastPingPosition;
    private Vector3 lastPingVelocity;
    private bool hasInitialPing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public static SensorDelayBuffer GetOrCreate(Rigidbody targetRb)
    {
        if (targetRb == null) return null;
        SensorDelayBuffer buf = targetRb.GetComponent<SensorDelayBuffer>();
        if (buf == null)
        {
            buf = targetRb.gameObject.AddComponent<SensorDelayBuffer>();
        }
        return buf;
    }

    /// <summary>
    /// Resets the buffer history and acoustic ping timer upon simulation teleportation/reset.
    /// </summary>
    public void ResetState()
    {
        buffer.Clear();
        dvlPingTimer = 0f;
        hasInitialPing = false;

        if (rb != null)
        {
            lastPingPosition = rb.position;
            lastPingVelocity = rb.linearVelocity;
            hasInitialPing = true;
            RecordSample(Time.time);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        RecordSample(Time.time);
        PruneBuffer(Time.time);
    }

    private void RecordSample(float currentTime)
    {
        dvlPingTimer += Time.fixedDeltaTime;

        // Throttled sampling for translation (Zero-Order Hold for DVL acoustic pings)
        if (!hasInitialPing || dvlPingTimer >= dvlPingInterval)
        {
            lastPingPosition = rb.position;
            lastPingVelocity = rb.linearVelocity;
            dvlPingTimer = 0f;
            hasInitialPing = true;
        }

        RigidbodyStateSample sample = new RigidbodyStateSample
        {
            timestamp = currentTime,
            position = lastPingPosition,        // Throttled (staircase) translation
            linearVelocity = lastPingVelocity,  // Throttled (staircase) translation
            rotation = rb.rotation,             // Unthrottled orientation (for 65+ Hz IMU)
            angularVelocity = rb.angularVelocity, // Unthrottled angular velocity (for IMU)
            unthrottledVelocity = rb.linearVelocity // Unthrottled velocity (for IMU linear acceleration)
        };

        buffer.Add(sample);
    }

    private void PruneBuffer(float currentTime)
    {
        // Maintain up to 2.0 seconds of history, pruning older samples
        while (buffer.Count > 1 && (currentTime - buffer[0].timestamp) > 2.0f)
        {
            buffer.RemoveAt(0);
        }
    }

    /// <summary>
    /// Retrieves the recorded Rigidbody state sample from delaySeconds ago.
    /// </summary>
    public RigidbodyStateSample GetDelayedState(float delaySeconds)
    {
        if (buffer.Count == 0)
        {
            if (rb != null)
            {
                return new RigidbodyStateSample
                {
                    timestamp = Time.time,
                    position = rb.position,
                    linearVelocity = rb.linearVelocity,
                    rotation = rb.rotation,
                    angularVelocity = rb.angularVelocity,
                    unthrottledVelocity = rb.linearVelocity
                };
            }
            return new RigidbodyStateSample { rotation = Quaternion.identity };
        }

        float targetTime = Time.time - delaySeconds;

        int bestIndex = 0;
        float minDiff = Mathf.Abs(buffer[0].timestamp - targetTime);

        for (int i = 1; i < buffer.Count; i++)
        {
            float diff = Mathf.Abs(buffer[i].timestamp - targetTime);
            if (diff < minDiff)
            {
                minDiff = diff;
                bestIndex = i;
            }
            else
            {
                // Timestamps are monotonically increasing; once diff increases, we found the closest sample
                break;
            }
        }

        return buffer[bestIndex];
    }

    /// <summary>
    /// Computes the delayed world position of a sensor located at localSensorOffset relative to the Rigidbody.
    /// Uses throttled (staircase) translation for DVL raycasting.
    /// </summary>
    public Vector3 GetDelayedSensorPosition(Vector3 localSensorOffset, float delaySeconds = 0.15f)
    {
        RigidbodyStateSample sample = GetDelayedState(delaySeconds);
        return sample.position + sample.rotation * localSensorOffset;
    }

    /// <summary>
    /// Computes delayed point velocity for a sensor at localSensorOffset using throttled (staircase) translation for DVL.
    /// </summary>
    public Vector3 GetThrottledDelayedVelocityAtLocalOffset(Vector3 localSensorOffset, float delaySeconds = 0.15f)
    {
        RigidbodyStateSample sample = GetDelayedState(delaySeconds);
        Vector3 r = sample.rotation * localSensorOffset;
        return sample.linearVelocity + Vector3.Cross(sample.angularVelocity, r);
    }

    /// <summary>
    /// Computes delayed point velocity for a sensor at localSensorOffset using unthrottled translation for IMU acceleration differentiation.
    /// </summary>
    public Vector3 GetUnthrottledDelayedVelocityAtLocalOffset(Vector3 localSensorOffset, float delaySeconds = 0.15f)
    {
        RigidbodyStateSample sample = GetDelayedState(delaySeconds);
        Vector3 r = sample.rotation * localSensorOffset;
        return sample.unthrottledVelocity + Vector3.Cross(sample.angularVelocity, r);
    }
}

public struct RigidbodyStateSample
{
    public float timestamp;
    public Vector3 position;
    public Vector3 linearVelocity;
    public Quaternion rotation;
    public Vector3 angularVelocity;
    public Vector3 unthrottledVelocity;
}
