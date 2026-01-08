using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;


public class CameraEnhancedSubscriber : MonoBehaviour
{
    private ROSConnection roscon;
    [Tooltip("Camera type determines which settings and topic to use")]
    public CameraType cameraType;
    private string topic;
    [HideInInspector] public Texture2D CurrentEnhancedTexture { get; private set; }
    private byte[] EnhancedRawData;
    private bool isMessageReceived = false;

    void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        // initialize texture with dummy size, will be replaced on first message with LoadImage call
        CurrentEnhancedTexture = new Texture2D(2, 2);
        if (cameraType == CameraType.Front) topic = ROSSettings.Instance.EnhancedFrontCameraTopic;
        else topic = ROSSettings.Instance.EnhancedDownCameraTopic;
        Debug.Log("[CameraEnhancedSubscriber] Subscribing to " + topic);
        roscon.Subscribe<CompressedImageMsg>(topic, ReceiveEnhancedImage);
    }

    void Update()
    {
        if (isMessageReceived)
        {
            CurrentEnhancedTexture.LoadImage(EnhancedRawData);
            isMessageReceived = false;
        }
    }

    private void ReceiveEnhancedImage(CompressedImageMsg imageMsg)
    {
        // Convert ROS ImageMsg to Texture2D
        EnhancedRawData = imageMsg.data;
        isMessageReceived = true;
    }

    void OnDestroy()
    {
        if (roscon != null)
        {
            roscon.Unsubscribe(topic);
        }
    }
}