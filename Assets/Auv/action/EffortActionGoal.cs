using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Auv
{
    public class EffortActionGoal : ActionGoal<EffortGoal>
    {
        public const string k_RosMessageName = "auv_msgs/EffortActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public EffortActionGoal() : base()
        {
            this.goal = new EffortGoal();
        }

        public EffortActionGoal(HeaderMsg header, GoalIDMsg goal_id, EffortGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static EffortActionGoal Deserialize(MessageDeserializer deserializer) => new EffortActionGoal(deserializer);

        EffortActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = EffortGoal.Deserialize(deserializer);
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
