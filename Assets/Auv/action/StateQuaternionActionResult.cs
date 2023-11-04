using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Auv
{
    public class StateQuaternionActionResult : ActionResult<StateQuaternionResult>
    {
        public const string k_RosMessageName = "auv_msgs/StateQuaternionActionResult";
        public override string RosMessageName => k_RosMessageName;


        public StateQuaternionActionResult() : base()
        {
            this.result = new StateQuaternionResult();
        }

        public StateQuaternionActionResult(HeaderMsg header, GoalStatusMsg status, StateQuaternionResult result) : base(header, status)
        {
            this.result = result;
        }
        public static StateQuaternionActionResult Deserialize(MessageDeserializer deserializer) => new StateQuaternionActionResult(deserializer);

        StateQuaternionActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = StateQuaternionResult.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.result);
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
