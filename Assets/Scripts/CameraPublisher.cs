using System;
using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class CameraPublisher : MonoBehaviour
{
	public Camera cam;

	public string imageTopic = "/vision/down_cam/image_raw";
	public string infoTopic = "/vision/down_cam/camera_info";

	public ROSClock ROSClock;

	public bool isFrontCam = false;

	private ROSConnection roscon;

	private string camPublishPreferenceKey, camFPSPreferenceKey, camWidthPreferenceKey, camHeightPreferenceKey;
	private bool publishToRos = true;
	private float timeSinceLastPublish, timeBetweenPublishes;

	private int publishWidth, publishHeight;
	private Rect publishRect;
	private Texture2D cameraTexture;
	private int imageStepSize = 4;
	private int imageStep;  // = publish width * image step size
	private ImageMsg imageMsg;
	private RenderTexture activeRenderTexture;


	private void Start()
	{
		// Start the ROS connection
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<ImageMsg>(imageTopic);
		roscon.RegisterPublisher<CameraInfoMsg>(infoTopic);
		Initialize();
	}

	private void Initialize()
	{
		// Set up image message that will be published to ROS
		imageMsg = new ImageMsg();
		imageMsg.encoding = "rgba8";
		HeaderMsg header = new HeaderMsg();
		imageMsg.header = header;

		// Determine which camera this is
		if (isFrontCam)
		{
			imageMsg.header.frame_id = "camera";
			camPublishPreferenceKey = "PublishFrontCamToggle";
			camFPSPreferenceKey = "frontCamRate";
			camWidthPreferenceKey = "frontCamWidth";
			camHeightPreferenceKey = "frontCamHeight";
		}
		else
		{
			imageMsg.header.frame_id = "down_cam";
			camPublishPreferenceKey = "PublishDownCamToggle";
			camFPSPreferenceKey = "downCamRate";
			camWidthPreferenceKey = "downCamWidth";
			camHeightPreferenceKey = "downCamHeight";
		}

		// Update publishing preferences and affected variables
		var publish = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true"))
									&& bool.Parse(PlayerPrefs.GetString(camPublishPreferenceKey, "true"));
		var fps = int.Parse(PlayerPrefs.GetString(camFPSPreferenceKey, "10"));
		var width = int.Parse(PlayerPrefs.GetString(camWidthPreferenceKey, "640"));
		var height = int.Parse(PlayerPrefs.GetString(camHeightPreferenceKey, "480"));
		SetPublishToRos(publish);
		SetPublishRate(fps);
		SetPublishResolution(width, height);
	}

	private void Update()
	{
		if (!publishToRos) return;
		timeSinceLastPublish += Time.deltaTime;
		if (timeSinceLastPublish < timeBetweenPublishes) return;

		// Publishing is enabled and enough time has elapsed since the last publish,
		// so publish the next image at the end of the frame
		StartCoroutine(WaitForEndOfFrameToPublish());
	}

	private IEnumerator WaitForEndOfFrameToPublish()
	{
		yield return new WaitForEndOfFrame();
		SendImage();
	}

	private void SendImage()
	{
		// Render the camera image to an offscreen texture (readonly from CPU side)
		var prevActiveRenderTexture = RenderTexture.active;
		RenderTexture.active = activeRenderTexture;
		cam.targetTexture = activeRenderTexture;

		// Render the camera image to the texture
		cam.Render();
		cameraTexture.ReadPixels(publishRect, 0, 0);
		cameraTexture.Apply();
		cam.targetTexture = prevActiveRenderTexture;
		cam.targetTexture = null;

		// Process the texture, update the image message, and publish
		imageMsg.data = FlipTextureVertically(cameraTexture, imageStep); 
		imageMsg.header.stamp.sec = (int)ROSClock.sec;
		imageMsg.header.stamp.nanosec = ROSClock.nanosec;
		roscon.Publish(imageTopic, imageMsg);

		// Also generate camera info and publish
		CameraInfoMsg cameraInfoMessage = CameraInfoGenerator.ConstructCameraInfoMessage(cam, imageMsg.header, 0.0f, 0.01f);
		cameraInfoMessage.width = (uint)publishWidth;
		cameraInfoMessage.height = (uint)publishHeight;
		cameraInfoMessage.K = GetIntrinsic(cam);
		roscon.Publish(infoTopic, cameraInfoMessage);

		// Reset time until next publish
		timeSinceLastPublish = 0;
	}

	public static byte[] FlipTextureVertically(Texture2D texture2D, int texRowSize)
	{
		byte[] imageData = texture2D.GetRawTextureData();

		// Loop through the top half of the rows, and swap with the equivalent row from the bottom half
		for (int y = 0; y < texture2D.height / 2; y++)
		{
			int topRowStart = y * texRowSize;
			int bottomRowStart = (texture2D.height - 1 - y) * texRowSize;

			// Swap top and bottom row pixels
			for (int i = 0; i < texRowSize; i++)
			{
				(imageData[topRowStart + i], imageData[bottomRowStart + i]) = (imageData[bottomRowStart + i], imageData[topRowStart + i]);
			}
		}

		return imageData;
	}

	private double[] GetIntrinsic(Camera cam)
	{
		// IntrinsicMatrix in row major
		var camIntrinsicMatrix = new double[9];

		camIntrinsicMatrix[0] = cam.focalLength * (publishWidth / cam.sensorSize.x);  // alpha_u
		camIntrinsicMatrix[1] = 0f;
		camIntrinsicMatrix[2] = publishWidth * 0.5f;  // u_0

		camIntrinsicMatrix[3] = 0f;
		camIntrinsicMatrix[4] = cam.focalLength * (publishWidth / (float)publishHeight) * (publishHeight / cam.sensorSize.y);  // alpha_v
		camIntrinsicMatrix[5] = publishHeight * 0.5f;  // v_0

		camIntrinsicMatrix[6] = 0f;
		camIntrinsicMatrix[7] = 0f;
		camIntrinsicMatrix[8] = 1f;

		return camIntrinsicMatrix;
	}

	public void SetPublishRate(int fps)
	{
		timeBetweenPublishes = fps > 0 ? 1.0f / fps : Mathf.Infinity;
	}

	public void SetPublishToRos(bool publish)
	{
		publishToRos = publish;
	}

	public void SetPublishResolution(int width, int height)
	{
		publishWidth = width;
		publishHeight = height;

		// Update the affected variables
		publishRect = new Rect(0, 0, publishWidth, publishHeight);
		cameraTexture = new Texture2D(publishWidth, publishHeight, TextureFormat.RGBA32, false);
		imageStep = imageStepSize * publishWidth;

		UpdateActiveRenderTexture();

		imageMsg.width = (uint)publishWidth;
		imageMsg.height = (uint)publishHeight;
		imageMsg.step = (uint)imageStep;
	}

	/// <summary>
	/// Sets the texture that the camera will render a frame to.
	/// </summary>
	private void UpdateActiveRenderTexture()
	{
		activeRenderTexture = new RenderTexture(publishWidth, publishHeight, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
		activeRenderTexture.Create();
	}
}