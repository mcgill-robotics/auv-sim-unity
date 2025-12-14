using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class PIDManager : MonoBehaviour
{
    public static PIDManager Instance { get; private set; }

    private ROSConnection roscon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        RegisterPublishers();
    }

    private void RegisterPublishers()
    {
        roscon.RegisterPublisher<Float64Msg>(ROSSettings.Instance.PidXSetpointTopic);
        roscon.RegisterPublisher<Float64Msg>(ROSSettings.Instance.PidYSetpointTopic);
        roscon.RegisterPublisher<Float64Msg>(ROSSettings.Instance.PidZSetpointTopic);
        roscon.RegisterPublisher<BoolMsg>(ROSSettings.Instance.PidQuatEnableTopic);
        roscon.RegisterPublisher<BoolMsg>(ROSSettings.Instance.PidXEnableTopic);
        roscon.RegisterPublisher<BoolMsg>(ROSSettings.Instance.PidYEnableTopic);
        roscon.RegisterPublisher<BoolMsg>(ROSSettings.Instance.PidZEnableTopic);
        roscon.RegisterPublisher<QuaternionMsg>(ROSSettings.Instance.PidQuatSetpointTopic);
    }

    public void SetPIDSetpoint(string axis, float value)
    {
        BoolMsg enableMsg = new BoolMsg(true);
        Float64Msg valMsg = new Float64Msg(value);

        switch (axis.ToLower())
        {
            case "x":
                roscon.Publish(ROSSettings.Instance.PidXEnableTopic, enableMsg);
                roscon.Publish(ROSSettings.Instance.PidXSetpointTopic, valMsg);
                break;
            case "y":
                roscon.Publish(ROSSettings.Instance.PidYEnableTopic, enableMsg);
                roscon.Publish(ROSSettings.Instance.PidYSetpointTopic, valMsg);
                break;
            case "z":
                roscon.Publish(ROSSettings.Instance.PidZEnableTopic, enableMsg);
                roscon.Publish(ROSSettings.Instance.PidZSetpointTopic, valMsg);
                break;
        }
    }

    public void SetRotationPID(float x, float y, float z)
    {
        BoolMsg enableMsg = new BoolMsg(true);
        roscon.Publish(ROSSettings.Instance.PidQuatEnableTopic, enableMsg);

        Quaternion setpoint = Quaternion.Euler(0f, 0f, -x) * Quaternion.Euler(-y, 0f, 0f) * Quaternion.Euler(0f, z, 0f);
        QuaternionMsg msg = setpoint.To<NED>();
        roscon.Publish(ROSSettings.Instance.PidQuatSetpointTopic, msg);
    }

    public void DisableAllPIDs()
    {
        BoolMsg bool_msg = new BoolMsg(false);
        roscon.Publish(ROSSettings.Instance.PidXEnableTopic, bool_msg);
        roscon.Publish(ROSSettings.Instance.PidYEnableTopic, bool_msg);
        roscon.Publish(ROSSettings.Instance.PidZEnableTopic, bool_msg);
        roscon.Publish(ROSSettings.Instance.PidQuatEnableTopic, bool_msg);
    }
}
