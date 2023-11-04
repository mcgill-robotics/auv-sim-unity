using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Auv
{
    public class EffortActionFeedback : ActionFeedback<EffortFeedback>
    {
        public const string k_RosMessageName = "auv_msgs/EffortActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public EffortActionFeedback() : base()
        {
            this.feedback = new EffortFeedback();
        }

        public EffortActionFeedback(HeaderMsg header, GoalStatusMsg status, EffortFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static EffortActionFeedback Deserialize(MessageDeserializer deserializer) => new EffortActionFeedback(deserializer);

        EffortActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = EffortFeedback.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.feedback);
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
