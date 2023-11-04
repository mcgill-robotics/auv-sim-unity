using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Auv
{
    public class StateQuaternionActionFeedback : ActionFeedback<StateQuaternionFeedback>
    {
        public const string k_RosMessageName = "auv_msgs/StateQuaternionActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public StateQuaternionActionFeedback() : base()
        {
            this.feedback = new StateQuaternionFeedback();
        }

        public StateQuaternionActionFeedback(HeaderMsg header, GoalStatusMsg status, StateQuaternionFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static StateQuaternionActionFeedback Deserialize(MessageDeserializer deserializer) => new StateQuaternionActionFeedback(deserializer);

        StateQuaternionActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = StateQuaternionFeedback.Deserialize(deserializer);
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
