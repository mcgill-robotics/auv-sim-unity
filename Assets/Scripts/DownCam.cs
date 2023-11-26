// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Rendering;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Sensor;

// /* TO-DO */

// public class Cameras : MonoBehaviour {
//     ROSConnection roscon;
//     public Camera downCam;

    
//     public string pubDownCamRaw = "/vision/down_cam/image_raw";
//     public string pubDownCamDepth = "/vision/front_cam/aligned_depth_to_color/camera_info";

//     public ImageMsg msgDownCamRaw;
//     public ImageMsg msgDownCamDepth;

//     public int imageWidth;
//     public int imageHeight;

//     RenderTexture renderTexture;
//     Texture2D texture;
    
    

//     // Start is called before the first frame update
//     void Start() {
//         roscon = ROSConnection.GetOrCreateInstance();

//         roscon.RegisterPublisher<ImageMsg>(pubDownCamRaw);
//         roscon.RegisterPublisher<ImageMsg>(pubDownCamDepth);

//         renderTexture = new RenderTexture(imageWidth, imageHeight);
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
//         msgDownCamRaw = camText.ToImageMsg();
//         roscon.Publish(topicName, msgDownCamRaw);
//         // camText.Dispose();
//     }

//     // Update is called once per frame
//     void Update() {
//         SendImage(downCam, pubDownCamRaw);
//     }
// }