using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Auv
{
    public class StateQuaternionActionGoal : ActionGoal<StateQuaternionGoal>
    {
        public const string k_RosMessageName = "auv_msgs/StateQuaternionActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public StateQuaternionActionGoal() : base()
        {
            this.goal = new StateQuaternionGoal();
        }

        public StateQuaternionActionGoal(HeaderMsg header, GoalIDMsg goal_id, StateQuaternionGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static StateQuaternionActionGoal Deserialize(MessageDeserializer deserializer) => new StateQuaternionActionGoal(deserializer);

        StateQuaternionActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = StateQuaternionGoal.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.goal_id);
            serializer.Write(this.goal);
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
