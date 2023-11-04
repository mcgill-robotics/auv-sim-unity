using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Auv
{
    public class StateQuaternionAction : Action<StateQuaternionActionGoal, StateQuaternionActionResult, StateQuaternionActionFeedback, StateQuaternionGoal, StateQuaternionResult, StateQuaternionFeedback>
    {
        public const string k_RosMessageName = "auv_msgs/StateQuaternionAction";
        public override string RosMessageName => k_RosMessageName;


        public StateQuaternionAction() : base()
        {
            this.action_goal = new StateQuaternionActionGoal();
            this.action_result = new StateQuaternionActionResult();
            this.action_feedback = new StateQuaternionActionFeedback();
        }

        public static StateQuaternionAction Deserialize(MessageDeserializer deserializer) => new StateQuaternionAction(deserializer);

        StateQuaternionAction(MessageDeserializer deserializer)
        {
            this.action_goal = StateQuaternionActionGoal.Deserialize(deserializer);
            this.action_result = StateQuaternionActionResult.Deserialize(deserializer);
            this.action_feedback = StateQuaternionActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
