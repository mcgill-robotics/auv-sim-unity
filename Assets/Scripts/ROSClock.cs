
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
	private LogicManager1 classLogicManager;
	private bool publishToRos = true;
	private ClockMsg message;
	private double clockTimePassed;

	private void Start()
	{
		// Start the ROS connection
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<ClockMsg>(this.topicName);

		// Set up the clock message
		message = new ClockMsg();
		message.clock.sec = 0;
		message.clock.nanosec = 0;

		clockTimePassed = double.Parse(PlayerPrefs.GetString("ROSClock", "0"));
		
		// Subscribe to toggle events
		classLogicManager = FindObjectOfType<LogicManager1>();
		if (classLogicManager != null)
		{
			classLogicManager.PublishROSToggle?.onValueChanged.AddListener(isOn => publishToRos = isOn);
		}
		else
		{
			Debug.LogError("[in StatePublisher.cs] LogiManager class is not assigned.");
		}
	}
	
	private void Update()
	{
		if (publishToRos) SendClock();
	}

	private void SendClock()
	{
		var publishTime = Time.fixedTimeAsDouble + clockTimePassed;

		message.clock.sec = (int)Math.Floor(publishTime);
		message.clock.nanosec = (uint)((publishTime - Math.Floor(publishTime)) * 1e9f);

		roscon.Publish(topicName, message);
		PlayerPrefs.SetString("ROSClock", publishTime.ToString());
	}

}