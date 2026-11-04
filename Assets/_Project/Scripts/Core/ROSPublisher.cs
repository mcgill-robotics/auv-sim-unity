using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

/// <summary>
/// Base class for ROS publishers. Implements IROSPublisher and provides ROS connection,
/// topic registration, and optional rate-limited publishing.
/// Publishers can extend this for default behavior, or implement IROSPublisher directly for full control.
/// </summary>
public abstract class ROSPublisher : MonoBehaviour, IROSPublisher
{
    [Header("ROS Configuration")]
    [Tooltip("Target publish rate in Hz (used if useBaseRateLimiting is true)")]
    [SerializeField] private float _publishRate = 10f;

    /// <summary>
    /// Topic name for this publisher. Must be implemented by subclasses.
    /// </summary>
    public abstract string Topic { get; }
    
    /// <summary>
    /// Target publish rate in Hz.
    /// </summary>
    public float PublishRate
    {
        get => _publishRate;
        set
        {
            _publishRate = Mathf.Clamp(value, 0.1f, 1000f);
            timeBetweenPublishes = 1f / _publishRate;
        }
    }
    
    /// <summary>
    /// Whether this publisher is currently active.
    /// </summary>
    public bool IsActive => shouldPublish;

    /// <summary>
    /// When true, base class handles publish timing. When false, subclass manages its own timing.
    /// Set to false if subclass needs custom timing (e.g., adaptive rates, physics-rate publishing).
    /// </summary>
    protected bool useBaseRateLimiting = true;

    protected ROSConnection ros;
    protected float timeBetweenPublishes;
    protected float timeSinceLastPublish;
    protected bool shouldPublish = true;

    protected virtual void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        timeBetweenPublishes = 1f / _publishRate;
        RegisterPublisher();
    }

    protected virtual void FixedUpdate()
    {
        // Only apply base rate limiting if subclass wants it
        if (!useBaseRateLimiting) return;
        
        timeSinceLastPublish += Time.fixedDeltaTime;
        if (timeSinceLastPublish < timeBetweenPublishes) return;

        // Subtract the period instead of resetting to 0, otherwise every publisher runs
        // slower than asked (e.g. 30 Hz on a 50 Hz physics step came out as 25 Hz).
        // If we fell more than one period behind (rate above the physics rate), drop the backlog.
        timeSinceLastPublish -= timeBetweenPublishes;
        if (timeSinceLastPublish >= timeBetweenPublishes) timeSinceLastPublish = 0f;

        bool rosEnabled = SimulationSettings.Instance == null || SimulationSettings.Instance.PublishROS;
        if (shouldPublish && rosEnabled)
        {
            PublishMessage();
        }
    }

    /// <summary>
    /// Register the publisher with ROS. Must be implemented by subclasses.
    /// </summary>
    protected abstract void RegisterPublisher();
    
    /// <summary>
    /// Publish a message to the ROS topic. Must be implemented by subclasses.
    /// </summary>
    public abstract void PublishMessage();

    /// <summary>
    /// Set the publish rate in Hz.
    /// </summary>
    public void SetPublishRate(float rate)
    {
        PublishRate = rate;
    }

    /// <summary>
    /// Enable or disable publishing.
    /// </summary>
    public void SetActive(bool active)
    {
        shouldPublish = active;
    }
}

