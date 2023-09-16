//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Auv
{
    [Serializable]
    public class DeadReckonReportMsg : Message
    {
        public const string k_RosMessageName = "auv_msgs/DeadReckonReport";
        public override string RosMessageName => k_RosMessageName;

        public double x;
        public double y;
        public double z;
        public double roll;
        public double pitch;
        public double yaw;
        public double std;
        public bool status;

        public DeadReckonReportMsg()
        {
            this.x = 0.0;
            this.y = 0.0;
            this.z = 0.0;
            this.roll = 0.0;
            this.pitch = 0.0;
            this.yaw = 0.0;
            this.std = 0.0;
            this.status = false;
        }

        public DeadReckonReportMsg(double x, double y, double z, double roll, double pitch, double yaw, double std, bool status)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.roll = roll;
            this.pitch = pitch;
            this.yaw = yaw;
            this.std = std;
            this.status = status;
        }

        public static DeadReckonReportMsg Deserialize(MessageDeserializer deserializer) => new DeadReckonReportMsg(deserializer);

        private DeadReckonReportMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.x);
            deserializer.Read(out this.y);
            deserializer.Read(out this.z);
            deserializer.Read(out this.roll);
            deserializer.Read(out this.pitch);
            deserializer.Read(out this.yaw);
            deserializer.Read(out this.std);
            deserializer.Read(out this.status);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.x);
            serializer.Write(this.y);
            serializer.Write(this.z);
            serializer.Write(this.roll);
            serializer.Write(this.pitch);
            serializer.Write(this.yaw);
            serializer.Write(this.std);
            serializer.Write(this.status);
        }

        public override string ToString()
        {
            return "DeadReckonReportMsg: " +
            "\nx: " + x.ToString() +
            "\ny: " + y.ToString() +
            "\nz: " + z.ToString() +
            "\nroll: " + roll.ToString() +
            "\npitch: " + pitch.ToString() +
            "\nyaw: " + yaw.ToString() +
            "\nstd: " + std.ToString() +
            "\nstatus: " + status.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
