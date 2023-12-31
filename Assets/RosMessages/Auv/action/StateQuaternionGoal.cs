//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Auv
{
    [Serializable]
    public class StateQuaternionGoal : Message
    {
        public const string k_RosMessageName = "auv_msgs/StateQuaternion";
        public override string RosMessageName => k_RosMessageName;

        //  goal
        public Geometry.PoseMsg pose;
        public Std.BoolMsg do_x;
        public Std.BoolMsg do_y;
        public Std.BoolMsg do_z;
        public Std.BoolMsg do_quaternion;
        public Std.BoolMsg displace;
        public Std.BoolMsg local;

        public StateQuaternionGoal()
        {
            this.pose = new Geometry.PoseMsg();
            this.do_x = new Std.BoolMsg();
            this.do_y = new Std.BoolMsg();
            this.do_z = new Std.BoolMsg();
            this.do_quaternion = new Std.BoolMsg();
            this.displace = new Std.BoolMsg();
            this.local = new Std.BoolMsg();
        }

        public StateQuaternionGoal(Geometry.PoseMsg pose, Std.BoolMsg do_x, Std.BoolMsg do_y, Std.BoolMsg do_z, Std.BoolMsg do_quaternion, Std.BoolMsg displace, Std.BoolMsg local)
        {
            this.pose = pose;
            this.do_x = do_x;
            this.do_y = do_y;
            this.do_z = do_z;
            this.do_quaternion = do_quaternion;
            this.displace = displace;
            this.local = local;
        }

        public static StateQuaternionGoal Deserialize(MessageDeserializer deserializer) => new StateQuaternionGoal(deserializer);

        private StateQuaternionGoal(MessageDeserializer deserializer)
        {
            this.pose = Geometry.PoseMsg.Deserialize(deserializer);
            this.do_x = Std.BoolMsg.Deserialize(deserializer);
            this.do_y = Std.BoolMsg.Deserialize(deserializer);
            this.do_z = Std.BoolMsg.Deserialize(deserializer);
            this.do_quaternion = Std.BoolMsg.Deserialize(deserializer);
            this.displace = Std.BoolMsg.Deserialize(deserializer);
            this.local = Std.BoolMsg.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.pose);
            serializer.Write(this.do_x);
            serializer.Write(this.do_y);
            serializer.Write(this.do_z);
            serializer.Write(this.do_quaternion);
            serializer.Write(this.displace);
            serializer.Write(this.local);
        }

        public override string ToString()
        {
            return "StateQuaternionGoal: " +
            "\npose: " + pose.ToString() +
            "\ndo_x: " + do_x.ToString() +
            "\ndo_y: " + do_y.ToString() +
            "\ndo_z: " + do_z.ToString() +
            "\ndo_quaternion: " + do_quaternion.ToString() +
            "\ndisplace: " + displace.ToString() +
            "\nlocal: " + local.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Goal);
        }
    }
}
