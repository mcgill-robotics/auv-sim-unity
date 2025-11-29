using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class ZED2iSimSender : MonoBehaviour
{
    [Header("ZED Streaming Configuration")]
    [Tooltip("Port for ZED SDK local streaming connection")]
    [Range(1024, 65535)]
    public int streamPort = 30000;
    
    [Tooltip("ZED camera serial number identifier")]
    public int serialNumber = 41116066; // ZED X Serial
    
    [Tooltip("Target streaming framerate (Hz). Will be clamped by ZED SDK limits")]
    [Range(1, 60)]
    public int targetFPS = 30;

    [Space(10)]
    [Header("Camera References")]
    [Tooltip("Assign THIS GameObject (Left Camera) here")]
    public Camera leftCamera;
    
    [Tooltip("Assign the Right Camera GameObject here")]
    public Camera rightCamera;

    [Space(10)]
    [Header("Coordinate System Mapping")]
    [Tooltip("Invert rotation Y axis to convert Unity LHS to ZED RHS")]
    public bool invertRotY = true;
    
    [Tooltip("Invert rotation X axis")]
    public bool invertRotX = true;
    
    [Tooltip("Invert rotation Z axis")]
    public bool invertRotZ = true;
    
    [Space(5)]
    [Tooltip("Invert acceleration Y axis. Unity static = +9.81 Y, Bridge flips Y internally, so we send +9.81 (False)")]
    public bool invertAccelY = false;
    
    [Tooltip("Invert acceleration X axis")]
    public bool invertAccelX = false;
    
    [Tooltip("Invert acceleration Z axis")]
    public bool invertAccelZ = false;

    // Camera settings are loaded from SimulationSettings
    private int targetWidth = 960;
    private int targetHeight = 600;

    // --- Internals ---
    // Physics State calculation without Rigidbody
    private Vector3 lastPosition;
    private Vector3 lastLinearVelocity;
    private Vector3 currentProperAccelLocal;
    
    private Quaternion initialRotationInv;
    private RenderTexture leftRT, rightRT, flipRT;
    private Texture2D texBufferLeft, texBufferRight;
    private bool isStreaming = false;
    private int streamerID = 0;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
    public Rigidbody rb;

    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string DLL_NAME = "sl_zed64";
    #else
        const string DLL_NAME = "sl_zed";
    #endif

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct StreamingParametersFlattened
    {
        public int mode;
        public float q0; public float q1; public float q2; public float q3;
        public float t0; public float t1; public float t2;
        public int image_width; public int image_height;
        public int codec_type;
        public ushort port; public short padding0;
        public int fps; public int serial_number;
        public byte alpha_channel_included;
        public byte padding1; public byte padding2; public byte padding3;
        public int input_format;
        public byte verbose;
        public byte padding4; public byte padding5; public byte padding6;
        public int transport_layer_mode;
    }

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int init_streamer(int id, ref StreamingParametersFlattened params_stream);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int stream_rgb(int id, IntPtr left, IntPtr right, long timestamp_ns, 
        float qw, float qx, float qy, float qz, 
        float ax, float ay, float az);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void close_streamer(int id);

    void Start()
    {

        // Disable automatic rendering
        if (leftCamera != null) leftCamera.enabled = false;
        if (rightCamera != null) rightCamera.enabled = false;
        
        // Check if ZED streaming is enabled in settings
        if (SimulationSettings.Instance != null && !SimulationSettings.Instance.StreamZEDCamera)
        {
            enabled = false;
            Debug.Log("[ZED Sim] Disabled (StreamZEDCamera = false in settings)");
            return;
        }

        // Load camera settings from SimulationSettings
        if (SimulationSettings.Instance != null)
        {
            targetWidth = SimulationSettings.Instance.FrontCamWidth;
            targetHeight = SimulationSettings.Instance.FrontCamHeight;
            targetFPS = SimulationSettings.Instance.FrontCamRate;
            Debug.Log("[ZED Sim] Using settings: " + targetWidth + "x" + targetHeight + " @" + targetFPS + " FPS");
        }
        rb.sleepThreshold = 0.0f;

        // Init physics state
        lastPosition = transform.position;
        lastLinearVelocity = Vector3.zero;
        
        // Zero out start rotation so ZED starts at Identity
        initialRotationInv = Quaternion.Inverse(transform.rotation);

        // Get Rigidbody
        // rb = GetComponentInParent<Rigidbody>();

        InitializeCameraCapture();

        StartCoroutine(InitializeNativeStreamer());
    }

    // --- MANUAL PHYSICS CALCULATION ---
    void FixedUpdate()
    {
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }
        float dt = Time.fixedDeltaTime;
        if (dt <= 0) return;

        /*
        Vector3 currentPos = transform.position;

        // 1. Calculate Velocity (v = dx / dt)
        Vector3 currentVelocity = (currentPos - lastPosition) / dt;

        // 2. Calculate Kinematic Acceleration (a = dv / dt)
        Vector3 worldAccel = (currentVelocity - lastVelocity) / dt;

        // 3. Calculate Proper Acceleration (What IMU feels)
        // a_proper = a_kinematic - gravity
        // Stationary: 0 - (-9.81) = +9.81 (Upwards)
        Vector3 properAccelWorld = worldAccel - Physics.gravity;

        // 4. Convert to Local Frame (Camera Space)
        currentProperAccelLocal = transform.InverseTransformDirection(properAccelWorld);

        // Update state for next frame
        lastPosition = currentPos;
        lastVelocity = currentVelocity;
        */

        if (rb == null) return;

        Vector3 currentVelocity = rb.linearVelocity;
        if (Time.fixedDeltaTime > 0)
        {
            Vector3 worldAccel = (currentVelocity - lastLinearVelocity) / Time.fixedDeltaTime;
            Vector3 properAccelWorld = worldAccel - Physics.gravity;
            currentProperAccelLocal = transform.InverseTransformDirection(properAccelWorld);
        }
        lastLinearVelocity = currentVelocity;
    }

    IEnumerator CaptureAndSend()
    {
        while (isStreaming)
        {
            yield return new WaitForSeconds(1.0f / targetFPS);
            yield return waitForEndOfFrame;

            // Manual Render
            if (leftCamera) leftCamera.Render();
            if (rightCamera) rightCamera.Render();

            // --- 1. Flip Images ---
            // Ensure flipRT is created
            if (flipRT == null || flipRT.width != targetWidth || flipRT.height != targetHeight)
            {
                if (flipRT != null) flipRT.Release();
                flipRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
                flipRT.enableRandomWrite = true;
                flipRT.Create();
            }

            Graphics.Blit(leftRT, flipRT, new Vector2(1, -1), new Vector2(0, 1));
            RenderTexture.active = flipRT;
            texBufferLeft.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            texBufferLeft.Apply();
            Graphics.Blit(rightRT, flipRT, new Vector2(1, -1), new Vector2(0, 1));
            RenderTexture.active = flipRT;
            texBufferRight.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            texBufferRight.Apply();
            RenderTexture.active = null;

            // --- 2. Orientation ---
            // Calculate Delta Rotation from startup
            Quaternion deltaRot = initialRotationInv * transform.rotation;

            // Unity (LHS) -> ZED (RHS)
            // We negate X, Y, Z components to map Left-Handed Y-Up to Right-Handed Y-Down
            float qx = invertRotX ? -deltaRot.x : deltaRot.x;
            float qy = invertRotY ? -deltaRot.y : deltaRot.y; 
            float qz = invertRotZ ? -deltaRot.z : deltaRot.z;
            float qw = deltaRot.w;

            // --- 3. Acceleration ---
            // We calculated Proper Accel in Unity Frame (+9.81 Y when static).
            // ZED Bridge needs to receive +9.81 so it can flip it internally to -9.81.
            // So we do NOT invert Y here (invertAccelY = false).
            float ax = invertAccelX ? -currentProperAccelLocal.x : currentProperAccelLocal.x;
            float ay = invertAccelY ? -currentProperAccelLocal.y : currentProperAccelLocal.y;
            float az = invertAccelZ ? -currentProperAccelLocal.z : currentProperAccelLocal.z;

            // Use System Time for smoother network sync
            long timestamp_ns = (long)((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds * 1_000_000);

            // Get reference to raw data without creating a new byte[]
            NativeArray<byte> rawLeft = texBufferLeft.GetRawTextureData<byte>();
            NativeArray<byte> rawRight = texBufferRight.GetRawTextureData<byte>();

            SendFrameToDLL(rawLeft, rawRight, timestamp_ns, qw, qx, qy, qz, ax, ay, az);
        }
    }

    private unsafe void SendFrameToDLL(NativeArray<byte> rawLeft, NativeArray<byte> rawRight, long timestamp_ns, float qw, float qx, float qy, float qz, float ax, float ay, float az)
    {
        // Pass the memory address directly to C++
        stream_rgb(streamerID, 
            (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawLeft), 
            (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawRight), 
            timestamp_ns, 
            qw, qx, qy, qz, 
            ax, ay, az);
    }


    IEnumerator InitializeNativeStreamer()
    {
        yield return new WaitForSeconds(1.0f);
        streamerID = UnityEngine.Random.Range(1, 9999);
        StreamingParametersFlattened p = new StreamingParametersFlattened();
        p.mode = 1; 
        p.image_width = targetWidth; p.image_height = targetHeight;
        p.port = (ushort)streamPort; p.fps = targetFPS; p.serial_number = serialNumber;
        p.codec_type = 0; p.alpha_channel_included = 0; p.input_format = 0; p.verbose = 0; 
        p.q3 = 1; // Identity extrinsics

        if (init_streamer(streamerID, ref p) == 1) {
            Debug.Log($"[ZED Sim] Streamer {streamerID} Started.");
            isStreaming = true;
            StartCoroutine(CaptureAndSend()); // Start the loop
        } else {
            close_streamer(streamerID);
        }
    }

    void InitializeCameraCapture()
    {
        leftRT = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32) { useMipMap = false, antiAliasing = 1 };
        rightRT = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32) { useMipMap = false, antiAliasing = 1 };
        
        leftCamera.targetTexture = leftRT;
        rightCamera.targetTexture = rightRT;
        
        // Set ZED FOV from settings
        float fov = SimulationSettings.Instance != null ? SimulationSettings.Instance.FrontCamFOV : 77.9f;
        leftCamera.fieldOfView = fov;
        rightCamera.fieldOfView = fov;

        texBufferLeft = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        texBufferRight = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
    }

    void LateUpdate() 
    { 
        // Coroutine loop handles timing now
    }
    void OnDestroy() 
    { 
        if (isStreaming) close_streamer(streamerID);
        if (flipRT != null) flipRT.Release();
    }
}