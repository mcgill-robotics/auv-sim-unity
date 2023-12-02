
// using UnityEngine;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Sensor;
// using RosMessageTypes.Std;
// using RosMessageTypes.BuiltinInterfaces;
// using System.Collections;
 
// /// <summary>
// ///
// /// </summary>
 
// [RequireComponent(typeof(ROSClockSubscriber))]
// public class CameraPublisherTest : MonoBehaviour
// {
//     ROSConnection ros;
//     public string imageTopic = "/camera_rect/image_rect";
//     public string camInfoTopic = "/camera_rect/camera_info";
 
//     public string CompressedImageTopic = "/camera_rect/image_rect_compressed";
 
//     public Camera target_camera;
 
//     public bool compressed = false;
 
//     public float pubMsgFrequency = 30f;
 
//     private float timeElapsed;
//     private RenderTexture renderTexture;
//     private RenderTexture lastTexture;
 
//     private Texture2D mainCameraTexture;
//     private Rect frame;
 
 
//     private int frame_width;
//     private int frame_height;
//     private const int isBigEndian = 0;
//     private uint image_step = 4;
//     TimeMsg lastTime;
 
//     private ROSClockSubscriber clock;
 
//     private ImageMsg img_msg;
//     private CameraInfoMsg infoCamera;
 
//     private HeaderMsg header;
 
//     void Start()
//     {
//         // start the ROS connection
//         ros = ROSConnection.instance;
 
//         if(ros)
//         {
//             ros.RegisterPublisher<ImageMsg>(imageTopic);
//             ros.RegisterPublisher<CompressedImageMsg>(CompressedImageTopic);
 
//             ros.RegisterPublisher<CameraInfoMsg>(camInfoTopic);
//             clock = GetComponent<ROSClockSubscriber>();
//         }
//         else
//         {
//             Debug.Log("No ros connection found.");
//         }
 
 
//         if (!target_camera)
//         {
//             target_camera = Camera.main;
//         }
 
//         if (Camera.main)
//         {
//             renderTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
//             renderTexture.Create();
 
//             frame_width = renderTexture.width;
//             frame_height = renderTexture.height;
 
//             frame = new Rect(0, 0, frame_width, frame_height);
 
//             mainCameraTexture = new Texture2D(frame_width, frame_height, TextureFormat.RGBA32, false);
 
//             header = new HeaderMsg();
 
//             img_msg = new ImageMsg();
 
//             img_msg.width = (uint) frame_width;
//             img_msg.height = (uint) frame_height;
//             img_msg.step = image_step * (uint) frame_width;
//             img_msg.encoding = "rgba8";
 
//             infoCamera = CameraInfoGenerator.ConstructCameraInfoMessage(target_camera, header);
 
//         }
//         else
//         {
//             Debug.Log("No camera found.");
//         }
//     }
 
//     private void Update()
//     {
//         if (Camera.main)
//         {
//             timeElapsed += Time.deltaTime;
 
//             if (timeElapsed > (1 / pubMsgFrequency))
//             {
//                 header.stamp = clock._time;
//                 infoCamera.header = header;
 
//                 img_msg.header = header;
//                 img_msg.data = get_frame_raw();
           
//                 ros.Send(imageTopic, img_msg);
//                 ros.Send(camInfoTopic, infoCamera);
 
//                 timeElapsed = 0;
//             }
//         }
//         else
//         {
//             Debug.Log("No camera found.");
//         }
 
//     }
 
//     private byte[] get_frame_raw()
//     {      
//         Camera.main.targetTexture = renderTexture;
//         lastTexture = RenderTexture.active;
 
//         RenderTexture.active = renderTexture;
//         Camera.main.Render();
 
//         mainCameraTexture.ReadPixels(frame, 0, 0);
//         mainCameraTexture.Apply();
 
//         Camera.main.targetTexture = lastTexture;
 
//         Camera.main.targetTexture = null;
 
//         return mainCameraTexture.GetRawTextureData();;
//     }
// }
 