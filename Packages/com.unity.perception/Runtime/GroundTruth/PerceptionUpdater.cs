using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// PerceptionUpdater is automatically spawned when the player starts and is used to coordinate and maintain
    /// static perception lifecycle behaviours.
    /// </summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(5)]
    class PerceptionUpdater : MonoBehaviour
    {
        static Camera[] s_CamerasEnabledInScene = new Camera[16];

        static IEnumerable<Camera> camerasEnabledInScene
        {
            get
            {
                var cameraCount = Camera.allCamerasCount;
                if (s_CamerasEnabledInScene == null || s_CamerasEnabledInScene.Length < cameraCount)
                    s_CamerasEnabledInScene = new Camera[cameraCount * 2];
                Camera.GetAllCameras(s_CamerasEnabledInScene);
                for (var i = 0; i < cameraCount; i++)
                    yield return s_CamerasEnabledInScene[i];
            }
        }

        /// <summary>
        /// An event that is invoked once when rendering begins each frame.
        /// </summary>
        internal static event Action<ScriptableRenderContext> beginFrameRendering;

        /// <summary>
        /// An event that is invoked once after all rendering has completed each frame.
        /// </summary>
        internal static event Action<ScriptableRenderContext> endFrameRendering;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var updaterObject = new GameObject("PerceptionUpdater");
            updaterObject.AddComponent<PerceptionUpdater>();
            updaterObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(updaterObject);

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            RenderPipelineManager.endContextRendering += OnEndContextRendering;
            Application.quitting += () =>
            {
                RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
                RenderPipelineManager.endContextRendering -= OnEndContextRendering;
            };
        }

        void LateUpdate()
        {
            LabelManager.singleton.RegisterPendingLabels();
            DatasetCapture.Update();
            ComputeBufferDisposer.ReleaseExpiredBuffers();
            ImageEncoder.ExecutePendingCallbacks();
        }

        void OnDestroy()
        {
            DatasetCapture.OnDestroy();
        }

        static void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
        {
            if (!CamerasAreTargetingTheGameView(cameras) && AtLeastOneCameraInSceneTargetingGameView())
                return;

            if (beginFrameRendering != null)
                beginFrameRendering.Invoke(ctx);
        }

        static void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
        {
            if (!CamerasAreTargetingTheGameView(cameras) && AtLeastOneCameraInSceneTargetingGameView())
                return;

            if (endFrameRendering != null)
                endFrameRendering.Invoke(ctx);

            if (PerceptionCamera.visualizedPerceptionCamera != null)
                AsyncGPUReadback.WaitAllRequests();

            BlitVisualizedPerceptionCameraToScreen(ctx);
        }

        /// <summary>
        /// RenderPipelineManager.endContextRendering can be called twice per frame: first for all cameras with
        /// render texture targets, and then again for all cameras without explicit render texture targets. Using the
        /// following check will ensure that PerceptionUpdater.endFrameRendering only runs once per frame.
        /// </summary>
        /// <param name="cameras">
        /// The camera argument passed through the RenderPipelineManager.endContextRendering event.
        /// </param>
        /// <returns>Whether the given cameras are targeting the game view or not.</returns>
        static bool CamerasAreTargetingTheGameView(List<Camera> cameras)
        {
            return cameras.Count <= 0 || cameras[0].targetTexture == null;
        }

        static bool AtLeastOneCameraInSceneTargetingGameView()
        {
            return camerasEnabledInScene.Any(camera => camera.targetTexture == null);
        }

        static void BlitVisualizedPerceptionCameraToScreen(ScriptableRenderContext ctx)
        {
            if (PerceptionCamera.enabledPerceptionCameras.Count == 0)
                return;

            PerceptionCamera highestPriorityCamera;
            if (PerceptionCamera.visualizedPerceptionCamera != null)
            {
                highestPriorityCamera = PerceptionCamera.visualizedPerceptionCamera;
            }
            else
            {
                highestPriorityCamera = PerceptionCamera.enabledPerceptionCameras.First();
                foreach (var camera in PerceptionCamera.enabledPerceptionCameras)
                    if (camera.attachedCamera.depth > highestPriorityCamera.attachedCamera.depth)
                        highestPriorityCamera = camera;
            }

            if (!NeedsBlitToScreen(highestPriorityCamera))
                return;

            // The RGB channel is enabled in PerceptionCamera.Start, which can run after the first frame renders.
            if (!highestPriorityCamera.TryGetChannel<RGBChannel>(out var rgbChannel) || rgbChannel.outputTexture == null)
                return;

            var cmd = CommandBufferPool.Get("Blit Visualized Perception Camera To Screen");
            cmd.Blit(rgbChannel.outputTexture, (RenderTexture)null);
            ctx.ExecuteCommandBuffer(cmd);
            ctx.Submit();
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// Returns true only when the screen would otherwise not show this camera's image.
        /// </summary>
        /// <remarks>
        /// The blit copies the RGB channel, which is captured by an AfterPostProcess custom pass, over the back
        /// buffer. That is only needed when a sensor has turned the attached camera into an HDRP fullscreen
        /// passthrough camera (super sampling, or the cubemap/fisheye sensor), because then HDRP draws nothing for
        /// it and the real image lives in an offscreen texture.
        ///
        /// In every other case HDRP has already drawn the camera to the screen, so the blit is redundant at best.
        /// On HDRP 17 (Unity 6) it is also harmful: HDRP sets SupportedRenderingFeatures.rendersUIOverlay and draws
        /// Screen Space Overlay canvases itself at the end of the camera render, after post processing. The blit runs
        /// after that, so it paints over every overlay canvas, including Perception's own segmentation overlay.
        /// If the PerceptionCamera renders into a targetTexture, the blit also replaces whatever the game view camera
        /// drew with the sensor image.
        /// </remarks>
        static bool NeedsBlitToScreen(PerceptionCamera perceptionCamera)
        {
            var camera = perceptionCamera.attachedCamera;
            if (camera == null || camera.targetTexture != null)
                return false;
#if HDRP_PRESENT
            return camera.TryGetComponent<HDAdditionalCameraData>(out var cameraData) && cameraData.fullscreenPassthrough;
#else
            return false;
#endif
        }
    }
}
