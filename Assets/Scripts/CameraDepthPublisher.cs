using System;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.Serialization;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.IO;

public class CameraDepthPublisher : MonoBehaviour
{
	ROSConnection roscon;
	public Camera cam;

	public string imageTopic = "vision/front_cam/aligned_depth_to_rgb/image_raw/compressed";

	public ROSClock ROSClock;

	[Header("Shader Setup")]
	public Shader uberReplacementShader;
	public float opticalFlowSensitivity = 1.0f;

	private bool publishToRos = true;
	private float timeSinceLastPublish, timeBetweenPublishes;

	private int publishWidth, publishHeight;
	private Rect publishRect;
	private Texture2D cameraTexture;
	private int imageStepSize = 4;
	private int imageStep;
	private ImageMsg imageMsg;
	private RenderTexture activeRenderTexture;
	
	private CapturePass capturePass = new CapturePass() { name = "_depth" };
	
	struct CapturePass
	{
		public string name;
		public bool supportsAntialiasing;
		public bool needsRescale;
		public CapturePass(string name_) { name = name_; supportsAntialiasing = true; needsRescale = false; camera = null; }
		public Camera camera;
	};

	static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacementMode mode, Color clearColor)
	{
		var cb = new CommandBuffer();
		cb.SetGlobalFloat("_OutputMode", (int)mode); // @TODO: CommandBuffer is missing SetGlobalInt() method
		cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
		cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
		cam.SetReplacementShader(shader, "");
		cam.backgroundColor = clearColor;
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.allowHDR = true;
		cam.allowMSAA = false;
	}
	public enum ReplacementMode
	{
		ObjectId = 0,
		CatergoryId = 1,
		DepthCompressed = 2,
		DepthMultichannel = 3,
		Normals = 4
	};
	
	
	private void Start()
	{
		// Start the ROS connection
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.RegisterPublisher<ImageMsg>(imageTopic);
		Initialize();
	}

	private void Initialize()
	{
		// Set up image message that will be published to ROS
		imageMsg = new ImageMsg();
		imageMsg.encoding = "32FC1";
		HeaderMsg header = new HeaderMsg();
		imageMsg.header = header;
		
		// Update publishing preferences and affected variables
		var publish = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true")) 
		              && bool.Parse(PlayerPrefs.GetString("PublishFrontCamToggle", "true"));
		var fps = int.Parse(PlayerPrefs.GetString("frontCamRate", "10"));
		var width = int.Parse(PlayerPrefs.GetString("frontCamWidth", "640"));
		var height = int.Parse(PlayerPrefs.GetString("frontCamHeight", "480"));
		SetPublishToRos(publish);
		SetPublishRate(fps);
		SetPublishResolution(width, height);
		
		// Set up the depth camera shader
		SetupCameraWithReplacementShader(cam, uberReplacementShader, ReplacementMode.DepthCompressed, Color.white);

		capturePass.camera = cam;  // Do we need this?
	}
	
	void Update()
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
		ScaleTexture(cameraTexture);
		imageMsg.data = CameraPublisher.FlipTextureVertically(cameraTexture, imageStep);
		imageMsg.header.stamp.sec = ROSClock.sec;
		imageMsg.header.stamp.nanosec = ROSClock.nanosec;
		roscon.Publish(imageTopic, imageMsg);

		// Reset time until next publish
		timeSinceLastPublish = 0;
	}

	private void ScaleTexture(Texture2D depthImage)
	{
		float near = cam.nearClipPlane;
		float far = cam.farClipPlane;
		float depth = far - near;

		// Normalize and scale the depth pixels appropriately
		var raw = depthImage.GetRawTextureData<float>(); 		// Returns NativeArray<float> which is more optimal
		for (int i = 0; i < raw.Length; ++i)
		{
			raw[i] = (near * far) / Mathf.Lerp(far, near, raw[i]);
		}

		depthImage.Apply(false, false);
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
		cameraTexture = new Texture2D(publishWidth, publishHeight, TextureFormat.RFloat, false);
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
		bool supportsAntialiasing = true;
		var depth = 32;
		var format = RenderTextureFormat.Default;
		var readWrite = RenderTextureReadWrite.Default;
		var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;
		activeRenderTexture = RenderTexture.GetTemporary(publishWidth, publishHeight, depth, format, readWrite, antiAliasing);
	}
}
