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
    public float brightnessFactor = 1.25f;

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
            colorTex.LoadRawTextureData(msg.data);
            colorTex.Apply();
        } else if (msg.encoding == "bgr8") {
            colorTex = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGB24, false);
            colorTex.LoadRawTextureData(msg.data);
            colorTex.Apply();
            SwitchBlueAndRedChannels(colorTex);
        } else {
            colorTex = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGBA32, false);
            colorTex.LoadRawTextureData(msg.data);
            colorTex.Apply();
        }

        BrightenTexture(colorTex, brightnessFactor);

        visualizationImageGUI.texture = colorTex;
        int newWidth = (int) msg.width * (90 / (int) msg.height);
        visualizationImageGUI.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 90);
    }

    public void SwitchBlueAndRedChannels(Texture2D texture)
    {
        // Ensure the texture is not null
        if (texture == null)
        {
            Debug.LogError("Texture is null!");
            return;
        }

        // Get the texture data
        Color[] pixels = texture.GetPixels();

        // Iterate through each pixel and switch the blue and red channels
        for (int i = 0; i < pixels.Length; i++)
        {
            float temp = pixels[i].r;
            pixels[i].r = pixels[i].b;
            pixels[i].b = temp;
        }

        // Apply the modified pixels back to the texture
        texture.SetPixels(pixels);

        // Apply changes to the texture
        texture.Apply();
    }

    public void BrightenTexture(Texture2D inputTexture, float brightnessFactor)
    {
        // Get the raw pixel data from the input texture
        Color[] pixels = inputTexture.GetPixels();

        // Loop through each pixel and adjust its brightness
        for (int i = 0; i < pixels.Length; i++)
        {
            // Ensure the color values are clamped between 0 and 1
            pixels[i].r = Mathf.Clamp01(pixels[i].r * brightnessFactor);
            pixels[i].g = Mathf.Clamp01(pixels[i].g * brightnessFactor);
            pixels[i].b = Mathf.Clamp01(pixels[i].b * brightnessFactor);
            pixels[i].a = Mathf.Clamp01(pixels[i].a * brightnessFactor);
        }

        // Apply the modified pixels back to the input texture
        inputTexture.SetPixels(pixels);
        inputTexture.Apply();
    }

}

