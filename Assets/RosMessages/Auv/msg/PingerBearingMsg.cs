//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Auv
{
    [Serializable]
    public class PingerBearingMsg : Message
    {
        public const string k_RosMessageName = "auv_msgs/PingerBearing";
        public override string RosMessageName => k_RosMessageName;

        public Geometry.Vector3Msg pinger1_bearing;
        public Geometry.Vector3Msg pinger2_bearing;
        public Geometry.Vector3Msg pinger3_bearing;
        public Geometry.Vector3Msg pinger4_bearing;
        public double state_x;
        public double state_y;

        public PingerBearingMsg()
        {
            this.pinger1_bearing = new Geometry.Vector3Msg();
            this.pinger2_bearing = new Geometry.Vector3Msg();
            this.pinger3_bearing = new Geometry.Vector3Msg();
            this.pinger4_bearing = new Geometry.Vector3Msg();
            this.state_x = 0.0;
            this.state_y = 0.0;
        }

        public PingerBearingMsg(Geometry.Vector3Msg pinger1_bearing, Geometry.Vector3Msg pinger2_bearing, Geometry.Vector3Msg pinger3_bearing, Geometry.Vector3Msg pinger4_bearing, double state_x, double state_y)
        {
            this.pinger1_bearing = pinger1_bearing;
            this.pinger2_bearing = pinger2_bearing;
            this.pinger3_bearing = pinger3_bearing;
            this.pinger4_bearing = pinger4_bearing;
            this.state_x = state_x;
            this.state_y = state_y;
        }

        public static PingerBearingMsg Deserialize(MessageDeserializer deserializer) => new PingerBearingMsg(deserializer);

        private PingerBearingMsg(MessageDeserializer deserializer)
        {
            this.pinger1_bearing = Geometry.Vector3Msg.Deserialize(deserializer);
            this.pinger2_bearing = Geometry.Vector3Msg.Deserialize(deserializer);
            this.pinger3_bearing = Geometry.Vector3Msg.Deserialize(deserializer);
            this.pinger4_bearing = Geometry.Vector3Msg.Deserialize(deserializer);
            deserializer.Read(out this.state_x);
            deserializer.Read(out this.state_y);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.pinger1_bearing);
            serializer.Write(this.pinger2_bearing);
            serializer.Write(this.pinger3_bearing);
            serializer.Write(this.pinger4_bearing);
            serializer.Write(this.state_x);
            serializer.Write(this.state_y);
        }

        public override string ToString()
        {
            return "PingerBearingMsg: " +
            "\npinger1_bearing: " + pinger1_bearing.ToString() +
            "\npinger2_bearing: " + pinger2_bearing.ToString() +
            "\npinger3_bearing: " + pinger3_bearing.ToString() +
            "\npinger4_bearing: " + pinger4_bearing.ToString() +
            "\nstate_x: " + state_x.ToString() +
            "\nstate_y: " + state_y.ToString();
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
