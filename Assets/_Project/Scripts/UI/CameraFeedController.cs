using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles the Camera drawer UI: camera feed selection, fullscreen toggle, and snapshot saving.
/// Extracted from SimulatorHUD for better separation of concerns.
/// </summary>
public class CameraFeedController
{
    // UI Elements
    private DropdownField dropdownCamTopic;
    private Image cameraFeedImage;
    private Image fullscreenCameraBackground;
    private Button btnFullscreen;
    private Button btnSaveImage;

    // Camera References
    private Camera frontLeftCamera;
    private Camera downCamera;
    private CameraDepthPublisher depthPublisher;
    private CameraEnhancedSubscriber frontEnhancedSubscriber;

    // State
    private bool isFullscreenCamera = false;
    private System.Action<string> logCallback;

    public CameraFeedController(VisualElement root, Camera frontLeft, Camera down, System.Action<string> log)
    {
        frontLeftCamera = frontLeft;
        downCamera = down;
        logCallback = log;

        QueryElements(root);
        RegisterCallbacks();
    }

    private void QueryElements(VisualElement root)
    {
        dropdownCamTopic = root.Q<DropdownField>("Dropdown-CamTopic");
        cameraFeedImage = root.Q<Image>("CameraFeedImage");
        fullscreenCameraBackground = root.Q<Image>("FullscreenCameraBackground");
        btnFullscreen = root.Q<Button>("Btn-Fullscreen");
        btnSaveImage = root.Q<Button>("Btn-SaveImage");
    }

    private void RegisterCallbacks()
    {
        if (btnFullscreen != null)
        {
            btnFullscreen.clicked += ToggleFullscreenCamera;
        }

        if (btnSaveImage != null)
        {
            btnSaveImage.clicked += OnSaveImage;
        }
    }

    /// <summary>
    /// Initialize camera dropdown with available feeds.
    /// Call this after camera publishers have initialized their textures.
    /// </summary>
    public void InitializeCameraDropdown(CameraDepthPublisher depth, CameraEnhancedSubscriber enhanced)
    {
        depthPublisher = depth;
        frontEnhancedSubscriber = enhanced;

        var choices = new List<string>();
        choices.Add("None"); // Allow disabling camera feed for performance
        if (frontLeftCamera != null) choices.Add("Front Left");
        if (downCamera != null) choices.Add("Down");
        if (depthPublisher != null) choices.Add("Front Depth");
        if (frontEnhancedSubscriber != null) choices.Add("Front Enhanced");

        if (dropdownCamTopic != null)
        {
            dropdownCamTopic.choices = choices;
            dropdownCamTopic.index = 0; // Start with None selected
            dropdownCamTopic.RegisterValueChangedCallback(evt => UpdateCameraFeed(evt.newValue));
        }

        // Initial update
        UpdateCameraFeed(choices[0]);
    }

    private void UpdateCameraFeed(string selection)
    {
        // Reset all feeds
        if (cameraFeedImage != null) cameraFeedImage.image = null;
        if (fullscreenCameraBackground != null) fullscreenCameraBackground.image = null;

        // Reset CameraRenderManager UI flags
        if (CameraRenderManager.Instance != null)
        {
            CameraRenderManager.Instance.frontCameraUINeeded = false;
            CameraRenderManager.Instance.frontDepthUINeeded = false;
            CameraRenderManager.Instance.downCameraUINeeded = false;
        }

        // Exit early if None selected (no camera feed)
        if (selection == "None")
        {
            // Disable fullscreen mode if active
            if (isFullscreenCamera)
            {
                ToggleFullscreenCamera();
            }
            return;
        }

        // Update CameraRenderManager UI flags for selected camera
        if (CameraRenderManager.Instance != null)
        {
            CameraRenderManager.Instance.frontCameraUINeeded = (selection == "Front Left");
            CameraRenderManager.Instance.frontDepthUINeeded = (selection == "Front Depth");
            CameraRenderManager.Instance.downCameraUINeeded = (selection == "Down");
            // since enhanced camera feed comes from a ROS topic independently, no need to set any camera renderer flags
        }

        Texture selectedTexture = null;

        switch (selection)
        {
            case "Front Left":
                if (frontLeftCamera != null)
                {
                    selectedTexture = frontLeftCamera.targetTexture;
                }
                break;
            case "Down":
                if (downCamera != null)
                {
                    selectedTexture = downCamera.targetTexture;
                }
                break;
            case "Front Depth":
                if (depthPublisher != null)
                {
                    selectedTexture = depthPublisher.VisualizationTexture;
                }
                break;
            case "Front Enhanced":
                if (frontEnhancedSubscriber != null)
                {
                    selectedTexture = frontEnhancedSubscriber.CurrentEnhancedTexture;
                }
                break;
        }

        // Set both preview and fullscreen background
        if (cameraFeedImage != null) cameraFeedImage.image = selectedTexture;
        if (fullscreenCameraBackground != null) fullscreenCameraBackground.image = selectedTexture;
    }

    private void ToggleFullscreenCamera()
    {
        if (fullscreenCameraBackground == null) return;

        isFullscreenCamera = !isFullscreenCamera;

        if (isFullscreenCamera)
        {
            fullscreenCameraBackground.style.display = DisplayStyle.Flex;
            if (btnFullscreen != null) btnFullscreen.text = "✕";
        }
        else
        {
            fullscreenCameraBackground.style.display = DisplayStyle.None;
            if (btnFullscreen != null) btnFullscreen.text = "⛶";
        }
    }

    public void OnSaveImage()
    {
        if (cameraFeedImage == null || cameraFeedImage.image == null)
        {
            logCallback?.Invoke("No camera feed to save.");
            return;
        }

        Texture sourceTexture = cameraFeedImage.image;
        RenderTexture renderTexture = sourceTexture as RenderTexture;

        if (renderTexture == null)
        {
            logCallback?.Invoke("Cannot save this type of texture.");
            return;
        }

        // Create a new Texture2D with the same dimensions
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // Remember currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the supplied RenderTexture as the active one
        RenderTexture.active = renderTexture;

        // Read the pixels from the RenderTexture to the Texture2D
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        // Restore previously active render texture
        RenderTexture.active = currentActiveRT;

        // Encode texture into PNG
        byte[] bytes = texture2D.EncodeToPNG();
        UnityEngine.Object.Destroy(texture2D);

        // Construct filename
        string feedName = dropdownCamTopic != null ? dropdownCamTopic.value.Replace(" ", "") : "Camera";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{feedName}_{renderTexture.width}x{renderTexture.height}_{timestamp}.png";

        // Ensure directory exists
        string savePath = SimulationSettings.Instance.SnapshotSavePath;
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string fullPath = Path.Combine(savePath, filename);
        File.WriteAllBytes(fullPath, bytes);

        logCallback?.Invoke($"Snapshot saved to: {fullPath}");
    }

    /// <summary>
    /// Cycles to the next available camera feed.
    /// </summary>
    public void CycleCamera()
    {
        if (dropdownCamTopic == null || dropdownCamTopic.choices == null || dropdownCamTopic.choices.Count <= 1) return;

        int nextIndex = (dropdownCamTopic.index + 1) % dropdownCamTopic.choices.Count;
        dropdownCamTopic.index = nextIndex;

        // This triggers the value changed callback which calls UpdateCameraFeed
        logCallback?.Invoke($"Switched to camera: {dropdownCamTopic.value}");
    }
}
