using RosMessageTypes.Sensor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Robotics.ROSTCPConnector;


public class PointCloudRenderer : MonoBehaviour
{
    ROSConnection roscon;
    Texture2D texColor;
    Texture2D texPosScale;
    VisualEffect vfx;
    uint resolution = 640;
    uint particleCount = 0;

    public float particleSize = 0.1f;
    bool toUpdate = false;
    public string pointCloudTopicName = "/vision/front_cam/point_cloud_raw";

    private void Start()
    {
        vfx = GetComponent<VisualEffect>();
    }

    private void Update()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PointCloud2Msg>(pointCloudTopicName, pointCloudCallback);
        if (toUpdate)
        {
            toUpdate = false;

            vfx.Reinit();
            vfx.SetUInt(Shader.PropertyToID("ParticleCount"), particleCount);
            vfx.SetTexture(Shader.PropertyToID("TexColor"), texColor);
            vfx.SetTexture(Shader.PropertyToID("TexPosScale"), texPosScale);
            vfx.SetUInt(Shader.PropertyToID("Resolution"), resolution);
        }
    }

    void pointCloudCallback(PointCloud2Msg pointCloudMsg)
    {
        long numParticles = pointCloudMsg.data.Length / pointCloudMsg.point_step;
        Vector3[] positions = new Vector3[numParticles];
        Color[] colors = new Color[numParticles];
        int index = 0;
        for (int i = 0; i + pointCloudMsg.point_step < pointCloudMsg.data.Length; i += (int)(pointCloudMsg.point_step))
        {
            float x = System.BitConverter.ToSingle(pointCloudMsg.data, i);
            float y = System.BitConverter.ToSingle(pointCloudMsg.data, i + 4);
            float z = System.BitConverter.ToSingle(pointCloudMsg.data, i + 8);
            float r = System.BitConverter.ToSingle(pointCloudMsg.data, i + 12);
            float g = System.BitConverter.ToSingle(pointCloudMsg.data, i + 16);
            float b = System.BitConverter.ToSingle(pointCloudMsg.data, i + 20);
            Vector3 position = new Vector3(x, y, z);
            Color color = new Color(r, g, b);
            positions[index] = position;
            colors[index] = color;
            index++;
        }
        SetParticles(positions, colors);
    }

    public void SetParticles(Vector3[] positions, Color[] colors)
    {
        texColor = new Texture2D(positions.Length > (int)resolution ? (int)resolution : positions.Length, Mathf.Clamp(positions.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
        texPosScale = new Texture2D(positions.Length > (int)resolution ? (int)resolution : positions.Length, Mathf.Clamp(positions.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
        int texWidth = texColor.width;
        int texHeight = texColor.height;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = x + y * texWidth;
                texColor.SetPixel(x, y, colors[index]);
                var data = new Color(positions[index].x, positions[index].y, positions[index].z, particleSize);
                texPosScale.SetPixel(x, y, data);
            }
        }

        texColor.Apply();
        texPosScale.Apply();
        particleCount = (uint)positions.Length;
        toUpdate = true;
    }
}
