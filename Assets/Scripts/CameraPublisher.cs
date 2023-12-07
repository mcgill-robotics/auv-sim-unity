using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class CameraPublisher : MonoBehaviour {
    ROSConnection roscon;
    public Camera downCam;
    public Camera frontCam;
    public int downCamFPS;
    public int frontCamFPS;
    
    public string downCamImageTopic = "/vision/down_cam/image_raw";
    public string downCamInfoTopic = "/vision/down_cam/camera_info";
    public string frontCamImageTopic = "/vision/front_cam/color/image_raw";
    public string frontCamDepthTopic = "/vision/front_cam/aligned_depth_to_color/image_raw";
    public string frontCamInfoTopic = "/vision/front_cam/aligned_depth_to_color/camera_info";

    private uint image_step = 4;

    // public int imageWidth;
    // public int imageHeight;
    RenderTexture renderTexture;
    RenderTexture lastTexture;
    Texture2D cameraTexture;

    // Start is called before the first frame update
    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();

        roscon.RegisterPublisher<ImageMsg>(downCamImageTopic);
        roscon.RegisterPublisher<ImageMsg>(frontCamImageTopic);
        roscon.RegisterPublisher<ImageMsg>(frontCamDepthTopic);

        InvokeRepeating("PublishDownCam", 0f, 1f / downCamFPS);
        InvokeRepeating("PublishFrontCam", 0f, 1f / frontCamFPS);

    }

    void SendImage(Camera sensorCamera, string topicName) {
        renderTexture = new RenderTexture(sensorCamera.pixelWidth, sensorCamera.pixelHeight, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        renderTexture.Create();

        int frame_width = renderTexture.width;
        int frame_height = renderTexture.height;

        Rect frame = new Rect(0, 0, frame_width, frame_height);
 
        cameraTexture = new Texture2D(frame_width, frame_height, TextureFormat.RGBA32, false);

        HeaderMsg header = new HeaderMsg();

        ImageMsg img_msg = new ImageMsg();

        img_msg.width = (uint) frame_width;
        img_msg.height = (uint) frame_height;
        img_msg.step = image_step * (uint) frame_width;
        img_msg.encoding = "rgba8";

        sensorCamera.targetTexture = renderTexture;
        lastTexture = RenderTexture.active;
 
        RenderTexture.active = renderTexture;
        sensorCamera.Render();
 
        cameraTexture.ReadPixels(frame, 0, 0);
        cameraTexture.Apply();
 
        sensorCamera.targetTexture = lastTexture;
        sensorCamera.targetTexture = null;

        img_msg.header = header;
        // TODO: this is a slow way of flipping the image vertically, should find a faster way of doing this
        int rowSize = (int) image_step * (int) frame_width;
        byte[] imageData = cameraTexture.GetRawTextureData();
        byte[] tempRow = new byte[rowSize];
        for (int y = 0; y < frame_height / 2; y++) {
            int rowIndex1 = y * rowSize;
            int rowIndex2 = (frame_height - 1 - y) * rowSize;

            for (int i = 0; i < rowSize; i++) {
                byte temp = imageData[rowIndex1 + i];
                imageData[rowIndex1 + i] = imageData[rowIndex2 + i];
                imageData[rowIndex2 + i] = temp;
            }
        }
        img_msg.data = imageData;
    
        roscon.Publish(topicName, img_msg);
    }

    // void SendCameraInfo() {
    //     // TODO
    //     // Create a new CameraInfoMsg
    //     CameraInfoMsg cameraInfoMsg = new CameraInfoMsg
    //     {
    //         header = new HeaderMsg { frame_id = frameId },
    //         width = Screen.width,
    //         height = Screen.height,
    //         distortion_model = "plumb_bob",
    //         D = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0 },
    //         K = new double[] { 525.0, 0.0, Screen.width / 2.0, 0.0, 525.0, Screen.height / 2.0, 0.0, 0.0, 1.0 },
    //         R = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 },
    //         P = new double[] { 525.0, 0.0, Screen.width / 2.0, 0.0, 0.0, 525.0, Screen.height / 2.0, 0.0, 0.0, 0.0, 1.0, 0.0 },
    //         binning_x = 0,
    //         binning_y = 0,
    //         roi = new RegionOfInterestMsg(),
    //     };

    //     // Publish the CameraInfoMsg
    //     cameraInfoPublisher.Publish(cameraInfoMsg);
    // }

    void PublishFrontCam() {
        SendImage(frontCam, frontCamImageTopic);
    }

    void PublishDownCam() {
        SendImage(downCam, downCamImageTopic);
    }
}