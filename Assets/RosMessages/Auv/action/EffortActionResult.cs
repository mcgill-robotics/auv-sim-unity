using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Auv
{
    public class EffortActionResult : ActionResult<EffortResult>
    {
        public const string k_RosMessageName = "auv_msgs/EffortActionResult";
        public override string RosMessageName => k_RosMessageName;


        public EffortActionResult() : base()
        {
            this.result = new EffortResult();
        }

        public EffortActionResult(HeaderMsg header, GoalStatusMsg status, EffortResult result) : base(header, status)
        {
            this.result = result;
        }
        public static EffortActionResult Deserialize(MessageDeserializer deserializer) => new EffortActionResult(deserializer);

        EffortActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = EffortResult.Deserialize(deserializer);
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
