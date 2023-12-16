
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Rosgraph;


public class ROSClock : MonoBehaviour
{
    public string topicName = "/clock";
    [Header("READ-ONLY")]
    public uint sec;
    public uint nanosec;
    
    private ROSConnection roscon;
    private ClockMsg message;
 
    void Start()
    {
        // setup ROS
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<ClockMsg>(this.topicName);

        // setup ROS Message
        message = new ClockMsg();
        message.clock.sec = 0;
        message.clock.nanosec = 0;
    }
 
 
   
    void PublishMessage()
    { 
        var publishTime = Time.fixedTimeAsDouble;
 
        sec = (uint) Math.Floor(publishTime);
        nanosec = (uint)((publishTime - Math.Floor(publishTime)) * 1e9f);
 
        message.clock.sec = sec;
        message.clock.nanosec = nanosec;

        roscon.Publish(topicName, message);
    }
 
    private void Update()
    {
        if (bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"))) PublishMessage();
    }
 
}