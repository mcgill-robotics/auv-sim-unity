using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.UI;


public class CameraEnhancedSubscriber : MonoBehaviour
{
    private ROSConnection roscon;
    [Tooltip("Camera type determines which settings and topic to use")]
    public CameraType cameraType;
    [HideInInspector] public Texture2D CurrentEnhancedTexture { get; private set; }
    private byte[] EnhancedRawData;
    private bool isMessageReceived = false;

    void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        string topic;
        if (cameraType == CameraType.Front) topic = ROSSettings.Instance.EnhancedFrontCameraTopic;
        else topic = ROSSettings.Instance.EnhancedDownCameraTopic;

        roscon.Subscribe<ImageMsg>(topic, ReceiveEnhancedImage);
    }

    void Update()
    {
        if (isMessageReceived)
        {
            CurrentEnhancedTexture.LoadRawTextureData(EnhancedRawData);
            CurrentEnhancedTexture.Apply();
            isMessageReceived = false;

        }
    }
    private void ReceiveEnhancedImage(ImageMsg imageMsg)
    {
        // Convert ROS ImageMsg to Texture2D
        EnhancedRawData = imageMsg.data;
        isMessageReceived = true;
        // the first time or if the size has changed, create a new texture
        if (CurrentEnhancedTexture == null || CurrentEnhancedTexture.width != (int)imageMsg.width || CurrentEnhancedTexture.height != (int)imageMsg.height)
        {
            CurrentEnhancedTexture = new Texture2D((int)imageMsg.width, (int)imageMsg.height, TextureFormat.RGB24, false);
        }
    }

}