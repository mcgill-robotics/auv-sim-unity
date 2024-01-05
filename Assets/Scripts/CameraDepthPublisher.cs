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
    ROSConnection ros;
    public Camera cam;

    public string imageTopic = "vision/front_cam/aligned_depth_to_rgb/image_raw/compressed";

    int publishWidth;
    int publishHeight;
    int FPS;
    
    public ROSClock ROSClock;

    [Header("Shader Setup")]
    public Shader uberReplacementShader;
    public float opticalFlowSensitivity = 1.0f;

    private float timeElapsed;
    private uint image_step = 4;
    private bool publishToRos = true;
    private ImageMsg img_msg;
    private CapturePass capturePass = new CapturePass() { name = "_depth" };
    private Texture2D texture2D;
    private Rect rect;
    private RenderTexture rt;
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
    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(imageTopic);
        Initialize();
    }

    public void Initialize()
    {
        publishWidth = int.Parse(PlayerPrefs.GetString("frontCamWidth", "640"));
        publishHeight = int.Parse(PlayerPrefs.GetString("frontCamHeight", "480"));
        texture2D = new Texture2D(publishWidth, publishHeight, TextureFormat.RFloat, false);
        rect = new Rect(0, 0, publishWidth, publishHeight);

        img_msg = new ImageMsg();
        img_msg.width = (uint) publishWidth;
        img_msg.height = (uint) publishHeight;
        img_msg.step = image_step * (uint) publishWidth;
        img_msg.encoding = "32FC1";
        HeaderMsg header = new HeaderMsg();
        img_msg.header = header;

        //set up camera shader
        SetupCameraWithReplacementShader(cam, uberReplacementShader, ReplacementMode.DepthCompressed, Color.white);

        capturePass.camera = cam;

        bool supportsAntialiasing = true;
        var depth = 32;
        var format = RenderTextureFormat.Default;
        var readWrite = RenderTextureReadWrite.Default;
        var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;
        rt = RenderTexture.GetTemporary(publishWidth, publishHeight, depth, format, readWrite, antiAliasing);
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

    private void SendImage()
    {
        publishToRos = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "true")) && bool.Parse(PlayerPrefs.GetString("PublishFrontCamToggle", "true"));
        timeElapsed += Time.deltaTime;
        FPS = int.Parse(PlayerPrefs.GetString("frontCamRate", "10"));
        if (FPS < 1) {
            publishToRos = false;
        }

        if (timeElapsed > 1.0f/FPS && publishToRos)
        {


            var prevActiveRT = RenderTexture.active;
            // render to offscreen texture (readonly from CPU side)
            RenderTexture.active = rt;
            cam.targetTexture = rt;
            
            cam.Render();
            texture2D.ReadPixels(rect, 0, 0);
            texture2D.Apply();
            cam.targetTexture = prevActiveRT;
            cam.targetTexture = null;

            int rowSize = (int) image_step * (int) publishWidth;
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

            img_msg.data = imageData;
            img_msg.header.stamp.sec = ROSClock.sec;
            img_msg.header.stamp.nanosec = ROSClock.nanosec;

            ros.Publish(imageTopic, img_msg);

            timeElapsed = 0;
/*            renderRT.DiscardContents();
            renderRT.Release();
            finalRT.DiscardContents();
            finalRT.Release();*/
        }
    }
}