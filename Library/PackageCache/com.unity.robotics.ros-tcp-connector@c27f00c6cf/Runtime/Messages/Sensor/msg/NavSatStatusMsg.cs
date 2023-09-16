//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Sensor
{
    [Serializable]
    public class NavSatStatusMsg : Message
    {
        public const string k_RosMessageName = "sensor_msgs/NavSatStatus";
        public override string RosMessageName => k_RosMessageName;

        //  Navigation Satellite fix status for any Global Navigation Satellite System.
        // 
        //  Whether to output an augmented fix is determined by both the fix
        //  type and the last time differential corrections were received.  A
        //  fix is valid when status >= STATUS_FIX.
        public const sbyte STATUS_NO_FIX = -1; //  unable to fix position
        public const sbyte STATUS_FIX = 0; //  unaugmented fix
        public const sbyte STATUS_SBAS_FIX = 1; //  with satellite-based augmentation
        public const sbyte STATUS_GBAS_FIX = 2; //  with ground-based augmentation
        public sbyte status;
        //  Bits defining which Global Navigation Satellite System signals were
        //  used by the receiver.
        public const ushort SERVICE_GPS = 1;
        public const ushort SERVICE_GLONASS = 2;
        public const ushort SERVICE_COMPASS = 4; //  includes BeiDou.
        public const ushort SERVICE_GALILEO = 8;
        public ushort service;

        public NavSatStatusMsg()
        {
            this.status = 0;
            this.service = 0;
        }

        public NavSatStatusMsg(sbyte status, ushort service)
        {
            this.status = status;
            this.service = service;
        }

        public static NavSatStatusMsg Deserialize(MessageDeserializer deserializer) => new NavSatStatusMsg(deserializer);

        private NavSatStatusMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.status);
            deserializer.Read(out this.service);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.status);
            serializer.Write(this.service);
        }

        public override string ToString()
        {
            return "NavSatStatusMsg: " +
            "\nstatus: " + status.ToString() +
            "\nservice: " + service.ToString();
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
