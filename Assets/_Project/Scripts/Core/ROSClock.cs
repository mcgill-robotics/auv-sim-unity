using UnityEngine;
using RosMessageTypes.Rosgraph;

public class ROSClock : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.ClockTopic;

    private ClockMsg clockMsg;
    private double clockTimePassed = 0;
    private static ROSClock _instance;
    
    // Static helper to get current ROS timestamp for other scripts
    public static RosMessageTypes.BuiltinInterfaces.TimeMsg GetROSTimestamp()
    {
        if (_instance != null)
        {
            int secs = (int)_instance.clockTimePassed;
            uint nsecs = (uint)((_instance.clockTimePassed - secs) * 1e9);
            return new RosMessageTypes.BuiltinInterfaces.TimeMsg { sec = secs, nanosec = nsecs };
        }
        return new RosMessageTypes.BuiltinInterfaces.TimeMsg();
    }
    
    /// <summary>
    /// Returns the current ROS clock time in nanoseconds (high precision for ZED/async operations).
    /// </summary>
    public static long GetROSTimestampNanoseconds()
    {
        if (_instance == null) return 0;
        return (long)(_instance.clockTimePassed * 1e9);
    }

    protected override void Start()
    {
        _instance = this;
        base.Start();
        clockMsg = new ClockMsg();
        clockMsg.clock = new RosMessageTypes.BuiltinInterfaces.TimeMsg();
        
        if (PlayerPrefs.HasKey("clockTimePassed"))
        {
            clockTimePassed = double.Parse(PlayerPrefs.GetString("clockTimePassed"));
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ClockMsg>(Topic);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void PublishMessage()
    {
        clockTimePassed += Time.fixedDeltaTime;
        int secs = (int)clockTimePassed;
        uint nsecs = (uint)((clockTimePassed - secs) * 1e9);
        
        clockMsg.clock.sec = secs;
        clockMsg.clock.nanosec = nsecs;
        
        ros.Publish(Topic, clockMsg);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetString("clockTimePassed", clockTimePassed.ToString());
        PlayerPrefs.Save();
    }
}