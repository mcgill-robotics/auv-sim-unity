using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

public abstract class ROSPublisher : MonoBehaviour
{
    [Header("ROS Configuration")]
    public float publishRate = 10f; // Hz

    protected abstract string Topic { get; }

    protected ROSConnection ros;
    protected float timeBetweenPublishes;
    protected float timeSinceLastPublish;
    protected bool shouldPublish = true;

    protected virtual void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        timeBetweenPublishes = 1f / publishRate;
        RegisterPublisher();
    }

    protected virtual void FixedUpdate()
    {
        timeSinceLastPublish += Time.fixedDeltaTime;
        if (shouldPublish && SimulationSettings.Instance.PublishROS && timeSinceLastPublish >= timeBetweenPublishes)
        {
            PublishMessage();
            timeSinceLastPublish = 0;
        }
    }

    protected abstract void RegisterPublisher();
    protected abstract void PublishMessage();

    public void SetPublishRate(float rate)
    {
        publishRate = Mathf.Clamp(rate, 0.1f, 100f);
        timeBetweenPublishes = 1f / publishRate;
    }

    public void SetActive(bool active)
    {
        shouldPublish = active;
    }
}
