//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Auv
{
    [Serializable]
    public class UnityStateMsg : Message
    {
        public const string k_RosMessageName = "auv_msgs/UnityState";
        public override string RosMessageName => k_RosMessageName;

        public Geometry.Vector3Msg position;
        public Geometry.QuaternionMsg orientation;
        public Geometry.Vector3Msg velocity;
        public Geometry.Vector3Msg angular_velocity;
        public Geometry.Vector3Msg hydrophones_distances;
        public bool isDVLActive;
        public bool isDepthSensorActive;
        public bool isIMUActive;

        public UnityStateMsg()
        {
            this.position = new Geometry.Vector3Msg();
            this.orientation = new Geometry.QuaternionMsg();
            this.velocity = new Geometry.Vector3Msg();
            this.angular_velocity = new Geometry.Vector3Msg();
            this.hydrophones_distances = new Geometry.Vector3Msg();
            this.isDVLActive = false;
            this.isDepthSensorActive = false;
            this.isIMUActive = false;
        }

        public UnityStateMsg(Geometry.Vector3Msg position, Geometry.QuaternionMsg orientation, Geometry.Vector3Msg velocity, Geometry.Vector3Msg angular_velocity, Geometry.Vector3Msg hydrophones_distances, bool isDVLActive, bool isDepthSensorActive, bool isIMUActive)
        {
            this.position = position;
            this.orientation = orientation;
            this.velocity = velocity;
            this.angular_velocity = angular_velocity;
            this.hydrophones_distances = hydrophones_distances;
            this.isDVLActive = isDVLActive;
            this.isDepthSensorActive = isDepthSensorActive;
            this.isIMUActive = isIMUActive;
        }

        public static UnityStateMsg Deserialize(MessageDeserializer deserializer) => new UnityStateMsg(deserializer);

        private UnityStateMsg(MessageDeserializer deserializer)
        {
            this.position = Geometry.Vector3Msg.Deserialize(deserializer);
            this.orientation = Geometry.QuaternionMsg.Deserialize(deserializer);
            this.velocity = Geometry.Vector3Msg.Deserialize(deserializer);
            this.angular_velocity = Geometry.Vector3Msg.Deserialize(deserializer);
            this.hydrophones_distances = Geometry.Vector3Msg.Deserialize(deserializer);
            deserializer.Read(out this.isDVLActive);
            deserializer.Read(out this.isDepthSensorActive);
            deserializer.Read(out this.isIMUActive);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.position);
            serializer.Write(this.orientation);
            serializer.Write(this.velocity);
            serializer.Write(this.angular_velocity);
            serializer.Write(this.hydrophones_distances);
            serializer.Write(this.isDVLActive);
            serializer.Write(this.isDepthSensorActive);
            serializer.Write(this.isIMUActive);
        }

        public override string ToString()
        {
            return "UnityStateMsg: " +
            "\nposition: " + position.ToString() +
            "\norientation: " + orientation.ToString() +
            "\nvelocity: " + velocity.ToString() +
            "\nangular_velocity: " + angular_velocity.ToString() +
            "\nhydrophones_distances: " + hydrophones_distances.ToString() +
            "\nisDVLActive: " + isDVLActive.ToString() +
            "\nisDepthSensorActive: " + isDepthSensorActive.ToString() +
            "\nisIMUActive: " + isIMUActive.ToString();
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
