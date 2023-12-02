// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Rendering;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Sensor;

// public class CameraPublisher : MonoBehaviour {
//     ROSConnection roscon;
//     public Camera downCam;
//     public Camera frontCam;

//     public int downCamFPS;
//     public int frontCamFPS;
    
//     public string downCamImageTopic = "/vision/down_cam/image_raw";
//     public string downCamInfoTopic = "/vision/down_cam/camera_info";
//     public string frontCamImageTopic = "/vision/front_cam/image_raw";
//     public string frontCamDepthTopic = "/vision/front_cam/aligned_depth_to_color/image_raw";
//     public string frontCamInfoTopic = "/vision/front_cam/aligned_depth_to_color/camera_info";

//     public int imageWidth;
//     public int imageHeight;

//     RenderTexture renderTexture;
//     Texture2D texture;

//     // Start is called before the first frame update
//     void Start() {
//         roscon = ROSConnection.GetOrCreateInstance();

//         roscon.RegisterPublisher<ImageMsg>(downCamImageTopic);
//         roscon.RegisterPublisher<ImageMsg>(frontCamImageTopic);
//         roscon.RegisterPublisher<ImageMsg>(frontCamDepthTopic);

//         InvokeRepeating("PublishDownCam", 0f, 1f / downCamFPS);
//         InvokeRepeating("PublishFrontCam", 0f, 1f / frontCamFPS);

//     }

//     void SendImage(Camera sensorCamera, string topicName) {
//         var oldRT = RenderTexture.active;
//         RenderTexture.active = sensorCamera.targetTexture;
//         sensorCamera.Render();
        
//         // Copy the pixels from the GPU into a texture so we can work with them
//         // For more efficiency you should reuse this texture, instead of creating and disposing them every time
//         Texture2D camText = new Texture2D(sensorCamera.targetTexture.width, sensorCamera.targetTexture.height);
//         camText.ReadPixels(new Rect(0, 0, sensorCamera.targetTexture.width, sensorCamera.targetTexture.height), 0, 0);
//         camText.Apply();
//         RenderTexture.active = oldRT;
        
//         // Encode the texture as an ImageMsg, and send to ROS
//         ImageMsg imageMsg = camText.ToImageMsg();
//         RosConnection.GetOrCreateInstance().Publish(topicName, imageMsg);
//         camText.Dispose();
//     }

//     void PublishFrontCam() {
//         SendImage(frontCam, frontCamImageTopic);
//     }

//     void PublishDownCam() {
//         SendImage(downCam, downCamImageTopic);
//     }
// }