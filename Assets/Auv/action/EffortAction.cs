using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Auv
{
    public class EffortAction : Action<EffortActionGoal, EffortActionResult, EffortActionFeedback, EffortGoal, EffortResult, EffortFeedback>
    {
        public const string k_RosMessageName = "auv_msgs/EffortAction";
        public override string RosMessageName => k_RosMessageName;


        public EffortAction() : base()
        {
            this.action_goal = new EffortActionGoal();
            this.action_result = new EffortActionResult();
            this.action_feedback = new EffortActionFeedback();
        }

        public static EffortAction Deserialize(MessageDeserializer deserializer) => new EffortAction(deserializer);

        EffortAction(MessageDeserializer deserializer)
        {
            this.action_goal = EffortActionGoal.Deserialize(deserializer);
            this.action_result = EffortActionResult.Deserialize(deserializer);
            this.action_feedback = EffortActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
