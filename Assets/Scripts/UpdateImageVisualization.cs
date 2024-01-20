using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine.UI;
using TMPro;

public class UpdateImageVisualization : MonoBehaviour
{
    ROSConnection ros;
    public RawImage visualizationImageGUI;
    public TMP_Dropdown topicDropdown;
    public Texture2D defaultTexture;
    public int takeNthFrame = 5;

    private int imageCount = 0;
    private int lastDropdownIndex;
    private Texture2D colorTex;
    private string previouslySubscribedTopic = "";


    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
    }

    public void updateTopic()
    {
        if (previouslySubscribedTopic != "") ros.Unsubscribe(previouslySubscribedTopic);

        if (topicDropdown.value != 0) {
            ros.Subscribe<ImageMsg>(topicDropdown.options[topicDropdown.value].text, ImageCb);
            previouslySubscribedTopic = topicDropdown.options[topicDropdown.value].text;
        } else {
            previouslySubscribedTopic = "";
        }
        visualizationImageGUI.texture = defaultTexture;
        imageCount = 0;
    }

    void ImageCb(ImageMsg msg)
    {
        if (imageCount != 0) {
            imageCount += 1;
            return;
        }
        if (msg.encoding == "32FC1") {
            colorTex = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RFloat, false);
        // } else if (msg.encoding == "32FC3") {
        //     colorTex = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGBFloat, false);
        } else {
            colorTex = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGBA32, false);
        }
        colorTex.LoadRawTextureData(msg.data);
        colorTex.Apply();
        visualizationImageGUI.texture = colorTex;
        int newWidth = (int) msg.width * (90 / (int) msg.height);
        visualizationImageGUI.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 90);
    }

}

