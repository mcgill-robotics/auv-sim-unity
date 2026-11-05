using UnityEngine;
using RosMessageTypes.Rosgraph;

public struct ROSTime
{
    public double sec;
    public ROSTime(double seconds)
    {
        sec = seconds;
    }
    public long GetSec() => (long)sec;
    public long GetMilliSec() => (long)(sec * 1e3);
    public long GetMicroSec() => (long)(sec * 1e6);
    public long GetNanoSec() => (long)(sec * 1e9);
}

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
    /// Zero-allocation helper that populates an existing TimeMsg instance with the current ROS timestamp.
    /// </summary>
    public static void PopulateROSTimestamp(RosMessageTypes.BuiltinInterfaces.TimeMsg target)
    {
        if (target == null) return;
        if (_instance != null)
        {
            int secs = (int)_instance.clockTimePassed;
            uint nsecs = (uint)((_instance.clockTimePassed - secs) * 1e9);
            target.sec = secs;
            target.nanosec = nsecs;
        }
        else
        {
            target.sec = 0;
            target.nanosec = 0;
        }
    }
    
    /// <summary>
    /// Returns the current ROS clock time in nanoseconds (high precision for ZED/async operations).
    /// </summary>
    public static long GetROSTimestampNanoseconds()
    {
        if (_instance == null) return 0;
        return (long)(_instance.clockTimePassed * 1e9);
    }

    /// <summary>
    /// Returns the current ROS clock time in ROSTime, which contains multiple representations (sec, millisec, microsec, nanosec) for convenience.
    /// </summary>
    public static ROSTime GetROSTime()
    {
        if (_instance == null) return new ROSTime(0);
        return new ROSTime(_instance.clockTimePassed);
    }

    private readonly ClockMsg[] clockMsgPool = new ClockMsg[3]
    {
        new ClockMsg { clock = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
        new ClockMsg { clock = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
        new ClockMsg { clock = new RosMessageTypes.BuiltinInterfaces.TimeMsg() }
    };
    private int clockPoolIdx = 0;

    protected override void Start()
    {
        _instance = this;
        base.Start();

        // Sim time starts at 0 every play session. It used to be persisted in PlayerPrefs,
        // which made /clock continue from wherever the last session (on that machine) stopped.
        PlayerPrefs.DeleteKey("clockTimePassed");
        clockTimePassed = 0;
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ClockMsg>(Topic);
    }

    protected override void FixedUpdate()
    {
        clockTimePassed += Time.fixedDeltaTime;
        base.FixedUpdate();
    }

    public override void PublishMessage()
    {
        int secs = (int)clockTimePassed;
        uint nsecs = (uint)((clockTimePassed - secs) * 1e9);
        
        var msg = clockMsgPool[clockPoolIdx];
        clockPoolIdx = (clockPoolIdx + 1) % clockMsgPool.Length;
        msg.clock.sec = secs;
        msg.clock.nanosec = nsecs;
        
        ros.Publish(Topic, msg);
    }

}