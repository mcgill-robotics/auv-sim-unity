using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;


public class CameraEnhancedSubscriber : MonoBehaviour
{
	private ROSConnection roscon;
	public Texture CurrentEnhancedTexture { get; private set; }

	void Start()
	{
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.Subscribe<ImageMsg>(ROSSettings.Instance.EnhancedCameraTopic, ReceiveEnhancedImage);
	}

	private void ReceiveEnhancedImage(ImageMsg imageMsg)
	{
		// Convert ROS ImageMsg to Texture2D
		Texture2D texture = new Texture2D((int)imageMsg.width, (int)imageMsg.height, TextureFormat.RGB24, false);
		texture.LoadRawTextureData(imageMsg.data);
		texture.Apply();
		CurrentEnhancedTexture = texture;
	}

}