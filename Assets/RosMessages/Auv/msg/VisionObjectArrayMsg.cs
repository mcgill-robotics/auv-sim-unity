//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Auv
{
    [Serializable]
    public class VisionObjectArrayMsg : Message
    {
        public const string k_RosMessageName = "auv_msgs/VisionObjectArray";
        public override string RosMessageName => k_RosMessageName;

        //  array containing vision objects
        public VisionObjectMsg[] array;

        public VisionObjectArrayMsg()
        {
            this.array = new VisionObjectMsg[0];
        }

        public VisionObjectArrayMsg(VisionObjectMsg[] array)
        {
            this.array = array;
        }

        public static VisionObjectArrayMsg Deserialize(MessageDeserializer deserializer) => new VisionObjectArrayMsg(deserializer);

        private VisionObjectArrayMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.array, VisionObjectMsg.Deserialize, deserializer.ReadLength());
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.WriteLength(this.array);
            serializer.Write(this.array);
        }

        public override string ToString()
        {
            return "VisionObjectArrayMsg: " +
            "\narray: " + System.String.Join(", ", array.ToList());
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
