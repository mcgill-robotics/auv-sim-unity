using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class CameraPublisher : MonoBehaviour {
    ROSConnection roscon;
    public Camera cam;
    
    public string imageTopic = "/vision/down_cam/image_raw";
    public string infoTopic = "/vision/down_cam/camera_info";

    public ROSClock ROSClock;

    public bool isFrontCam = false;

    private uint image_step = 4;

    private float timeElapsed;
    RenderTexture renderTexture;
    RenderTexture lastTexture;
    Texture2D cameraTexture;
    Rect frame;
    int rowSize;
    ImageMsg img_msg;
    int publishHeight;
    int publishWidth;
    int FPS;
    string camPublishPreferenceKey;
    string camFPSPreferenceKey;
    bool publishToRos = true;

    // Start is called before the first frame update
    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<ImageMsg>(imageTopic);
        roscon.RegisterPublisher<CameraInfoMsg>(infoTopic);
        Initialize();
    }

    public void Initialize() {
        if (isFrontCam) {
            camPublishPreferenceKey = "PublishFrontCamToggle";
            camFPSPreferenceKey = "frontCamRate";
            publishWidth = int.Parse(PlayerPrefs.GetString("frontCamWidth", "640"));
            publishHeight = int.Parse(PlayerPrefs.GetString("frontCamHeight", "480"));
        }
        else {
            camPublishPreferenceKey = "PublishDownCamToggle";
            camFPSPreferenceKey = "downCamRate";
            publishWidth = int.Parse(PlayerPrefs.GetString("downCamWidth", "640"));
            publishHeight = int.Parse(PlayerPrefs.GetString("downCamHeight", "480"));
        }

        renderTexture = new RenderTexture(publishWidth, publishHeight, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        renderTexture.Create();

        cam.targetTexture = renderTexture;

        rowSize = (int) image_step * (int) publishWidth;

        frame = new Rect(0, 0, publishWidth, publishHeight);
 
        cameraTexture = new Texture2D(publishWidth, publishHeight, TextureFormat.RGBA32, false);

        img_msg = new ImageMsg();
        img_msg.width = (uint) publishWidth;
        img_msg.height = (uint) publishHeight;
        img_msg.step = image_step * (uint) publishWidth;
        img_msg.encoding = "rgba8";
        HeaderMsg header = new HeaderMsg();
        img_msg.header = header;
    }

    void Update()
    {
        StartCoroutine(WaitForEndOfFrameToPublish());
    }

    private IEnumerator WaitForEndOfFrameToPublish()
    {
        yield return new WaitForEndOfFrame();
        SendImage();
    }

    void SendImage() {
        publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true")) && bool.Parse(PlayerPrefs.GetString(camPublishPreferenceKey, "true"));
        
        FPS = int.Parse(PlayerPrefs.GetString(camFPSPreferenceKey, "10"));
        if (FPS < 1) {
            publishToRos = false;
        }
        timeElapsed += Time.deltaTime;
        if (timeElapsed > 1.0f/FPS && publishToRos)
        {
            cam.targetTexture = renderTexture;
            lastTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            cam.Render();
            cameraTexture.ReadPixels(frame, 0, 0);
            cameraTexture.Apply();
            cam.targetTexture = lastTexture;
            cam.targetTexture = null;

            byte[] imageData = flipTextureVertically(publishHeight, cameraTexture);

            img_msg.data = imageData;
            img_msg.header.stamp.sec = ROSClock.sec;
            img_msg.header.stamp.nanosec = ROSClock.nanosec;

            roscon.Publish(imageTopic, img_msg);
            CameraInfoMsg cameraInfoMessage = CameraInfoGenerator.ConstructCameraInfoMessage(cam, img_msg.header, 0.0f, 0.01f);
            cameraInfoMessage.width = (uint) publishWidth;
            cameraInfoMessage.height = (uint) publishHeight;
            roscon.Publish(infoTopic, cameraInfoMessage);
        }
    }

    private byte[] flipTextureVertically(int publishHeight, Texture2D texture2D) {
        byte[] imageData = texture2D.GetRawTextureData();
        for (int y = 0; y < publishHeight / 2; y++) {
            int rowIndex1 = y * rowSize;
            int rowIndex2 = (publishHeight - 1 - y) * rowSize;

            for (int i = 0; i < rowSize; i++) {
                byte temp = imageData[rowIndex1 + i];
                imageData[rowIndex1 + i] = imageData[rowIndex2 + i];
                imageData[rowIndex2 + i] = temp;
            }
        }

        return imageData;
    }
}