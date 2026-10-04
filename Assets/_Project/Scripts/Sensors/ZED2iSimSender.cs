using System;
using System.Collections;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ZED2iSimSender : MonoBehaviour
{
    [Header("ZED Streaming Configuration")]
    [Tooltip("Must be an EVEN port (e.g., 30000, 30002)")]
    [Range(1024, 65534)] public int streamPort = 30000;
    public int serialNumber = 0; // Set to 0 to auto-pick the first valid ZED 2i serial
    [Range(1, 60)] public int targetFPS = 30;
    public bool useSimTime = false;

    [Header("Camera References")]
    public Camera leftCamera;
    public Camera rightCamera;

    [Header("Resolution (ZED 2i Narrow VGA: 672x376)")]
    public int targetWidth = 672;
    public int targetHeight = 376;

    [Tooltip("Vertical Field of View (ZED 2i Narrow 4mm lens is ~40.9 deg)")]
    public float targetFOV = 40.9f;

    [Header("Underwater Optics & Refraction")]
    [Tooltip("Simulate flat-port water refraction (Snell's Law) on both stereo cameras")]
    public bool simulateRefraction = true;

    [Tooltip("Refractive index of water (standard water is ~1.33333)")]
    [Range(1.0f, 1.6f)]
    public float refractionIndex = 1.33333f;

    [Tooltip("Custom shader for flat-port Snell's law refraction warp")]
    public Shader refractionShader;
    private Material refractionMaterial;

    [Header("Coordinate System Mapping")]
    public bool invertRotX = true;
    public bool invertRotY = false;
    public bool invertRotZ = true;

    public bool invertAccelX = false;
    public bool invertAccelY = false;
    public bool invertAccelZ = false;

    [Header("Debug")]
    public bool sendOrientation = true;
    public bool debugLogging = false;
    [Range(1, 300)] public int debugLogInterval = 60;

    [SerializeField] private Rigidbody rbOverride;
    private Rigidbody Rb => rbOverride != null ? rbOverride : SimulationSettings.Instance?.AUVRigidbody;

    private Vector3 lastLinearVelocity;
    private Vector3 currentProperAccelLocal;
    private Vector3 currentAngularVelocityLocal;
    private Quaternion initialRotationInv;

    public RenderTexture RefractedLeftTexture => leftRT;
    public RenderTexture RefractedRightTexture => rightRT;

    private RenderTexture leftRT, rightRT, rawLeftRT, rawRightRT, flipLeftRT, flipRightRT;

    private Thread encodingThread;
    private volatile bool isStreaming = false;
    private const int streamerID = 0; // Streamer ID must start at 0
    private int frameCount = 0;
    private static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private NativeArray<byte>[] leftBuffers = new NativeArray<byte>[2];
    private NativeArray<byte>[] rightBuffers = new NativeArray<byte>[2];
    private long[] timestamps = new long[2];
    private Quaternion[] rotations = new Quaternion[2];
    private Vector3[] accelerations = new Vector3[2];

    private int encodeIndex = 1;
    private bool newFrameReady = false;
    private bool isEncoding = false;
    private readonly object frameLock = new object();

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        ApplyRefraction(cam);
    }

    public void ApplyRefraction(Camera cam)
    {
        if (!simulateRefraction || refractionMaterial == null) return;

        if (cam == leftCamera && leftRT != null && rawLeftRT != null)
        {
            UpdateRefractionMaterial(flipY: false);
            Graphics.Blit(leftRT, rawLeftRT);
            Graphics.Blit(rawLeftRT, leftRT, refractionMaterial);
        }
        else if (cam == rightCamera && rightRT != null && rawRightRT != null)
        {
            UpdateRefractionMaterial(flipY: false);
            Graphics.Blit(rightRT, rawRightRT);
            Graphics.Blit(rawRightRT, rightRT, refractionMaterial);
        }
    }

    void Start()
    {
        InitializeMemoryAndCameras();

        if (streamPort % 2 != 0)
        {
            streamPort--;
            Debug.LogWarning($"[ZED Sim] Port adjusted to even number: {streamPort}");
        }

        // 1. Verify Host ZED SDK Version
        if (ZedNativeAPI.getZEDSDKRuntimeVersion_C(out int major, out int minor, out int patch) == 0)
        {
            Debug.Log($"[ZED Sim] Host ZED SDK Runtime: v{major}.{minor}.{patch}");
            if (major < 5 || (major == 5 && (minor < 4 || (minor == 4 && patch < 1))))
            {
                Debug.LogError($"[ZED Sim] Incompatible ZED SDK! Requires >= 5.4.1, found {major}.{minor}.{patch}. Native streaming disabled.");
                return;
            }
        }

        // 2. Resolve Valid Serial Number
        ResolveSerialNumber();

        // 3. Ensure SimulationSettings knows ZED streaming is active so CameraRenderManager renders both cameras
        if (SimulationSettings.Instance != null)
        {
            SimulationSettings.Instance.StreamZEDCamera = true;
        }

        if (Rb != null) Rb.sleepThreshold = 0.0f;
        initialRotationInv = Quaternion.Inverse(transform.rotation);

        StartCoroutine(InitializeNativeStreamer());
    }

    void ResolveSerialNumber()
    {
        var virtualCams = ZedNativeAPI.GetVirtualCameras();
        Debug.Log($"[ZED Sim] Found {virtualCams.Length} virtual cameras in SDK library.");

        if (serialNumber != 0 && ZedNativeAPI.is_sn_valid(serialNumber))
        {
            Debug.Log($"[ZED Sim] Using specified serial number: {serialNumber}");
            return;
        }

        foreach (var cam in virtualCams)
        {
            // model == 3 (ZED 2i) AND lens_type == 1 (Narrow)
            if (cam.model == 3 && cam.lens_type == 1)
            {
                serialNumber = cam.serial_number;
                Debug.Log($"[ZED Sim] Auto-selected ZED 2i NARROW Serial: {serialNumber}");
                return;
            }
        }

        // Fallback to any model 3 if narrow lens not found
        foreach (var cam in virtualCams)
        {
            if (cam.model == 3)
            {
                serialNumber = cam.serial_number;
                Debug.LogWarning($"[ZED Sim] Fallback to ZED 2i Serial (lens {cam.lens_type}): {serialNumber}");
                return;
            }
        }

        Debug.LogError("[ZED Sim] No virtual ZED 2i NARROW serial number found in library!");
    }

    void InitializeMemoryAndCameras()
    {
        if (leftCamera == null) leftCamera = GetComponent<Camera>();
        if (rightCamera == null && transform.parent != null)
        {
            var rc = transform.parent.Find("Right_Camera");
            if (rc != null) rightCamera = rc.GetComponent<Camera>();
        }

        if (leftCamera == null || rightCamera == null)
        {
            Debug.LogError("[ZED Sim] Left or Right Camera reference is missing!");
            enabled = false;
            return;
        }

        leftCamera.fieldOfView = targetFOV;
        rightCamera.fieldOfView = targetFOV;

        leftRT = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32) { useMipMap = false };
        rightRT = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32) { useMipMap = false };
        rawLeftRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32) { useMipMap = false };
        rawRightRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32) { useMipMap = false };
        flipLeftRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32) { enableRandomWrite = true };
        flipRightRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32) { enableRandomWrite = true };

        leftCamera.targetTexture = leftRT;
        rightCamera.targetTexture = rightRT;

        int bufferSize = targetWidth * targetHeight * 3;
        for (int i = 0; i < 2; i++)
        {
            leftBuffers[i] = new NativeArray<byte>(bufferSize, Allocator.Persistent);
            rightBuffers[i] = new NativeArray<byte>(bufferSize, Allocator.Persistent);
        }

        InitializeRefraction();
    }

    private void InitializeRefraction()
    {
        if (SimulationSettings.Instance != null)
        {
            simulateRefraction = SimulationSettings.Instance.SimulateWaterRefraction;
            refractionIndex = SimulationSettings.Instance.WaterRefractionIndex;
        }

        if (refractionShader == null)
        {
            refractionShader = Shader.Find("Hidden/UnderwaterRefraction");
        }

        if (refractionShader != null)
        {
            refractionMaterial = new Material(refractionShader);
            Debug.Log($"[ZED Sim] Underwater refraction initialized (n={refractionIndex:F4}, enabled={simulateRefraction}).");
        }
        else
        {
            Debug.LogWarning("[ZED Sim] Hidden/UnderwaterRefraction shader not found! Fallback to standard blit.");
        }
    }

    private void UpdateRefractionMaterial(bool flipY = false)
    {
        if (refractionMaterial == null) return;

        if (SimulationSettings.Instance != null)
        {
            simulateRefraction = SimulationSettings.Instance.SimulateWaterRefraction;
            refractionIndex = SimulationSettings.Instance.WaterRefractionIndex;
        }

        // Focal length derived from vertical FOV and targetHeight: f = (H/2) / tan(FOV/2)
        float fy = (targetHeight * 0.5f) / Mathf.Tan(targetFOV * 0.5f * Mathf.Deg2Rad);
        float fx = fy; // Square pixels

        refractionMaterial.SetVector("_Resolution", new Vector4(targetWidth, targetHeight, 0, 0));
        refractionMaterial.SetVector("_FocalLength", new Vector4(fx, fy, 0, 0));
        refractionMaterial.SetVector("_PrincipalPoint", new Vector4(0.5f, 0.5f, 0, 0));
        refractionMaterial.SetFloat("_RefractionIndex", refractionIndex);
        refractionMaterial.SetFloat("_Enabled", simulateRefraction ? 1.0f : 0.0f);
        refractionMaterial.SetFloat("_FlipY", flipY ? 1.0f : 0.0f);
    }

    IEnumerator InitializeNativeStreamer()
    {
        yield return new WaitForSeconds(1.0f);

        var p = ZedNativeAPI.StreamingParameters.CreateDefault(
            targetWidth, targetHeight, targetFPS, (ushort)streamPort, serialNumber);

        if (ZedNativeAPI.InitStreamer(streamerID, ref p))
        {
            Debug.Log($"[ZED Sim] Streamer {streamerID} Started on port {streamPort} (SN: {serialNumber}, {targetWidth}x{targetHeight} @ {targetFPS} FPS).");
            isStreaming = true;

            encodingThread = new Thread(EncodingWorkerThread);
            encodingThread.Start();

            StartCoroutine(CaptureLoop());
        }
        else
        {
            Debug.LogError($"[ZED Sim] Streamer {streamerID} Failed to Start on port {streamPort}. Check if port is already bound or serial/resolution is invalid.");
            ZedNativeAPI.CloseStreamer(streamerID);
        }
    }

    void FixedUpdate()
    {
        if (Rb == null || !isStreaming) return;
        if (Rb.IsSleeping()) Rb.WakeUp();

        float dt = Time.fixedDeltaTime;
        if (dt <= 0) return;

        Vector3 currentVelocity = Rb.linearVelocity;
        Vector3 worldAccel = (currentVelocity - lastLinearVelocity) / dt;
        Vector3 properAccelWorld = worldAccel - Physics.gravity;

        currentProperAccelLocal = transform.InverseTransformDirection(properAccelWorld);
        currentAngularVelocityLocal = transform.InverseTransformDirection(Rb.angularVelocity) * Mathf.Rad2Deg;
        lastLinearVelocity = currentVelocity;

        SendIMUData();
    }

    private void SendIMUData()
    {
        long ts = useSimTime ? ROSClock.GetROSTimestampNanoseconds() : (long)((DateTime.UtcNow - epochStart).TotalMilliseconds * 1_000_000);

        Quaternion deltaRot = sendOrientation ? (initialRotationInv * transform.rotation) : Quaternion.identity;
        Quaternion rot = new Quaternion(
            invertRotX ? -deltaRot.x : deltaRot.x,
            invertRotY ? -deltaRot.y : deltaRot.y,
            invertRotZ ? -deltaRot.z : deltaRot.z,
            deltaRot.w);

        Vector3 acc = new Vector3(
            invertAccelX ? -currentProperAccelLocal.x : currentProperAccelLocal.x,
            invertAccelY ? -currentProperAccelLocal.y : currentProperAccelLocal.y,
            invertAccelZ ? -currentProperAccelLocal.z : currentProperAccelLocal.z);

        Vector3 angVel = new Vector3(
            invertRotX ? -currentAngularVelocityLocal.x : currentAngularVelocityLocal.x,
            invertRotY ? -currentAngularVelocityLocal.y : currentAngularVelocityLocal.y,
            invertRotZ ? -currentAngularVelocityLocal.z : currentAngularVelocityLocal.z);

        ZedNativeAPI.IngestIMU(streamerID, ts, angVel, acc, rot);
    }

    IEnumerator CaptureLoop()
    {
        while (isStreaming)
        {
            yield return new WaitForEndOfFrame();

            Graphics.Blit(leftRT, flipLeftRT, new Vector2(1, -1), new Vector2(0, 1));
            Graphics.Blit(rightRT, flipRightRT, new Vector2(1, -1), new Vector2(0, 1));

            long ts = useSimTime ? ROSClock.GetROSTimestampNanoseconds() : (long)((DateTime.UtcNow - epochStart).TotalMilliseconds * 1_000_000);

            Quaternion deltaRot = sendOrientation ? (initialRotationInv * transform.rotation) : Quaternion.identity;
            Quaternion rot = new Quaternion(
                invertRotX ? -deltaRot.x : deltaRot.x,
                invertRotY ? -deltaRot.y : deltaRot.y,
                invertRotZ ? -deltaRot.z : deltaRot.z,
                deltaRot.w);

            Vector3 acc = new Vector3(
                invertAccelX ? -currentProperAccelLocal.x : currentProperAccelLocal.x,
                invertAccelY ? -currentProperAccelLocal.y : currentProperAccelLocal.y,
                invertAccelZ ? -currentProperAccelLocal.z : currentProperAccelLocal.z);

            var reqLeft = AsyncGPUReadback.Request(flipLeftRT, 0, TextureFormat.RGB24);
            var reqRight = AsyncGPUReadback.Request(flipRightRT, 0, TextureFormat.RGB24);

            StartCoroutine(WaitForReadbacks(reqLeft, reqRight, ts, rot, acc));

            yield return new WaitForSeconds(1.0f / targetFPS);
        }
    }

    IEnumerator WaitForReadbacks(AsyncGPUReadbackRequest reqL, AsyncGPUReadbackRequest reqR, long ts, Quaternion rot, Vector3 acc)
    {
        while (!reqL.done || !reqR.done) yield return null;
        if (reqL.hasError || reqR.hasError || !isStreaming) yield break;

        lock (frameLock)
        {
            int captureIndex = 1 - encodeIndex;

            reqL.GetData<byte>().CopyTo(leftBuffers[captureIndex]);
            reqR.GetData<byte>().CopyTo(rightBuffers[captureIndex]);

            timestamps[captureIndex] = ts;
            rotations[captureIndex] = rot;
            accelerations[captureIndex] = acc;

            newFrameReady = true;
            if (!isEncoding) Monitor.Pulse(frameLock);
        }

        if (debugLogging && (frameCount % debugLogInterval == 0))
            Debug.LogFormat("[ZED Debug] Frame {0}: Quat({1:F3}) | Accel({2:F2}) m/s²", frameCount, rot.w, acc.x);

        frameCount++;
    }

    private void EncodingWorkerThread()
    {
        while (isStreaming)
        {
            lock (frameLock)
            {
                while (!newFrameReady && isStreaming) Monitor.Wait(frameLock);
                if (!isStreaming) break;

                encodeIndex = 1 - encodeIndex;
                newFrameReady = false;
                isEncoding = true;
            }

            ZedNativeAPI.StreamRGB(streamerID,
                leftBuffers[encodeIndex], rightBuffers[encodeIndex],
                timestamps[encodeIndex], rotations[encodeIndex], accelerations[encodeIndex]);

            lock (frameLock)
            {
                isEncoding = false;
            }
        }
    }

    void OnDestroy()
    {
        isStreaming = false;

        lock (frameLock) Monitor.Pulse(frameLock);
        if (encodingThread != null && encodingThread.IsAlive) encodingThread.Join();

        ZedNativeAPI.CloseStreamer(streamerID);
        ZedNativeAPI.DestroyInstance();

        for (int i = 0; i < 2; i++)
        {
            if (leftBuffers[i].IsCreated) leftBuffers[i].Dispose();
            if (rightBuffers[i].IsCreated) rightBuffers[i].Dispose();
        }

        if (leftRT != null) { leftRT.Release(); Destroy(leftRT); }
        if (rightRT != null) { rightRT.Release(); Destroy(rightRT); }
        if (rawLeftRT != null) { rawLeftRT.Release(); Destroy(rawLeftRT); }
        if (rawRightRT != null) { rawRightRT.Release(); Destroy(rawRightRT); }
        if (flipLeftRT != null) { flipLeftRT.Release(); Destroy(flipLeftRT); }
        if (flipRightRT != null) { flipRightRT.Release(); Destroy(flipRightRT); }
        if (refractionMaterial != null) Destroy(refractionMaterial);
    }
}