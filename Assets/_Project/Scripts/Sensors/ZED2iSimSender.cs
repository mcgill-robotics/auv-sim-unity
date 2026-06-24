using System;
using System.Collections;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ZED2iSimSender : MonoBehaviour
{
    [Header("ZED Streaming Configuration")]
    [Range(1024, 65535)] public int streamPort = 30000;
    public int serialNumber = 47890353;
    [Range(1, 60)] public int targetFPS = 30;
    public bool useSimTime = false;

    [Header("Camera References")]
    public Camera leftCamera;
    public Camera rightCamera;

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

    [Tooltip("AUV Rigidbody - leave empty to use SimulationSettings.AUVRigidbody")]
    [SerializeField] private Rigidbody rbOverride;
    private Rigidbody Rb => rbOverride != null ? rbOverride : SimulationSettings.Instance?.AUVRigidbody;

    // Camera settings
    private int targetWidth = 960;
    private int targetHeight = 600;

    // Physics State
    private Vector3 lastLinearVelocity;
    private Vector3 currentProperAccelLocal;
    private Vector3 currentAngularVelocityLocal;
    private Quaternion initialRotationInv;

    // Rendering
    private RenderTexture leftRT, rightRT, flipLeftRT, flipRightRT;

    // Threading & Double Buffering
    private Thread encodingThread;
    private volatile bool isStreaming = false;
    private int streamerID = 0;
    private int frameCount = 0;
    private static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Buffers for the background thread
    private NativeArray<byte>[] leftBuffers = new NativeArray<byte>[2];
    private NativeArray<byte>[] rightBuffers = new NativeArray<byte>[2];
    private long[] timestamps = new long[2];
    private Quaternion[] rotations = new Quaternion[2];
    private Vector3[] accelerations = new Vector3[2];

    private int encodeIndex = 1;
    private bool newFrameReady = false;
    private bool isEncoding = false;
    private readonly object frameLock = new object();

    void Start()
    {
        if (SimulationSettings.Instance != null && !SimulationSettings.Instance.StreamZEDCamera)
        {
            enabled = false;
            return;
        }

        if (SimulationSettings.Instance != null)
        {
            targetWidth = SimulationSettings.Instance.FrontCamWidth;
            targetHeight = SimulationSettings.Instance.FrontCamHeight;
            targetFPS = SimulationSettings.Instance.FrontCamRate;
        }

        if (Rb != null) Rb.sleepThreshold = 0.0f;
        initialRotationInv = Quaternion.Inverse(transform.rotation);

        InitializeMemoryAndCameras();
        StartCoroutine(InitializeNativeStreamer());
    }

    void InitializeMemoryAndCameras()
    {
        leftRT = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32) { useMipMap = false };
        rightRT = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32) { useMipMap = false };
        flipLeftRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32) { enableRandomWrite = true };
        flipRightRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32) { enableRandomWrite = true };

        leftCamera.targetTexture = leftRT;
        rightCamera.targetTexture = rightRT;

        float fov = SimulationSettings.Instance != null ? SimulationSettings.Instance.FrontCamFOV : 52.0f;
        leftCamera.fieldOfView = fov;
        rightCamera.fieldOfView = fov;

        int bufferSize = targetWidth * targetHeight * 3;
        for (int i = 0; i < 2; i++)
        {
            leftBuffers[i] = new NativeArray<byte>(bufferSize, Allocator.Persistent);
            rightBuffers[i] = new NativeArray<byte>(bufferSize, Allocator.Persistent);
        }
    }

    IEnumerator InitializeNativeStreamer()
    {
        yield return new WaitForSeconds(1.0f);
        streamerID = UnityEngine.Random.Range(1, 9999);

        // Uses our clean wrapper to build the parameters
        var p = ZedNativeAPI.StreamingParameters.CreateDefault(
            targetWidth, targetHeight, targetFPS, (ushort)streamPort, serialNumber);

        if (ZedNativeAPI.InitStreamer(streamerID, ref p))
        {
            Debug.Log($"[ZED Sim] Streamer {streamerID} Started.");
            isStreaming = true;

            encodingThread = new Thread(EncodingWorkerThread);
            encodingThread.Start();

            StartCoroutine(CaptureLoop());
        }
        else
        {
            Debug.LogError($"[ZED Sim] Streamer {streamerID} Failed to Start.");
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

        // Clean API call
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

            // Clean API call handles all pointer logic internally
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
        if (flipLeftRT != null) { flipLeftRT.Release(); Destroy(flipLeftRT); }
        if (flipRightRT != null) { flipRightRT.Release(); Destroy(flipRightRT); }
    }
}