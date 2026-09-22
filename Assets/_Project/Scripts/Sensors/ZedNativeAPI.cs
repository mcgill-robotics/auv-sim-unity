using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class ZedNativeAPI
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const string DLL_NAME = "sl_zed64";
#else
    private const string DLL_NAME = "sl_zed";
#endif

    public enum CodecType { H264 = 0, H265 = 1 }
    public enum InputFormat { RGB = 0, BGR = 1, YUV = 2 }
    public enum TransportMode { Network = 0, IPC = 1, Both = 2 }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimCameraInfo
    {
        public int serial_number;
        public int model; // 1 = ZED_M, 3 = ZED_2i, 4 = ZED_X, 5 = ZED_XM, 9 = ZED_X_Nano
        public int lens_type; // 0 = Wide, 1 = Narrow, 2 = Fisheye
    }

    // Explicit 100-byte layout matching types_c.h in ZED SDK 5.4
    [StructLayout(LayoutKind.Explicit, Size = 100)]
    public struct StreamingParameters
    {
        [FieldOffset(0)]  public int mode;
        [FieldOffset(4)]  public float qx;
        [FieldOffset(8)]  public float qy;
        [FieldOffset(12)] public float qz;
        [FieldOffset(16)] public float qw;
        [FieldOffset(20)] public float tx;
        [FieldOffset(24)] public float ty;
        [FieldOffset(28)] public float tz;
        [FieldOffset(32)] public int image_width;
        [FieldOffset(36)] public int image_height;
        [FieldOffset(40)] public CodecType codec_type;
        [FieldOffset(44)] public ushort port;
        [FieldOffset(48)] public int fps;
        [FieldOffset(52)] public int serial_number;
        [FieldOffset(56)] public byte alpha_channel_included;
        [FieldOffset(60)] public InputFormat input_format;
        [FieldOffset(64)] public byte verbose;
        [FieldOffset(68)] public TransportMode transport_layer_mode;
        [FieldOffset(72)] public int bitrate;
        [FieldOffset(76)] public ushort chunk_size;

        // --- Fields added in ZED SDK 5.4 ---
        [FieldOffset(80)] public int gop_size;
        [FieldOffset(84)] public byte gpu_input;
        [FieldOffset(85)] public byte stream_depth;
        [FieldOffset(88)] public int depth_width;
        [FieldOffset(92)] public int depth_height;
        [FieldOffset(96)] public int depth_bitrate;

        public static StreamingParameters CreateDefault(int width, int height, int fps, ushort port, int serialNum)
        {
            return new StreamingParameters
            {
                mode = 1,
                qw = 1f,
                image_width = width,
                image_height = height,
                codec_type = CodecType.H265, // ZED SDK 5.4 default
                port = port,
                fps = fps,
                serial_number = serialNum,
                alpha_channel_included = 0, // 0 = RGB24
                input_format = InputFormat.RGB,
                verbose = 0,
                transport_layer_mode = TransportMode.Both, // Enables Network + IPC
                bitrate = 8000,
                chunk_size = 4096,
                gop_size = -1,
                gpu_input = 0,   // Must be 0 for CPU host memory
                stream_depth = 0,
                depth_width = 0,
                depth_height = 0,
                depth_bitrate = 0
            };
        }
    }

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int init_streamer(int id, ref StreamingParameters params_stream);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int stream_rgb(int id, IntPtr left, IntPtr right, long timestamp_ns,
        float qw, float qx, float qy, float qz, float ax, float ay, float az);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ingest_imu(int id, long timestamp_ns,
        float vx, float vy, float vz, float ax, float ay, float az, float qw, float qx, float qy, float qz);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void close_streamer(int id);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void destroy_instance();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool is_sn_valid(int serial_number);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_virtual_camera_info(out int size_out);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int getZEDSDKRuntimeVersion_C(out int major, out int minor, out int patch);

    public static bool InitStreamer(int id, ref StreamingParameters parameters)
        => init_streamer(id, ref parameters) == 1;

    public static void CloseStreamer(int id)
        => close_streamer(id);

    public static void DestroyInstance()
        => destroy_instance();

    public static void IngestIMU(int id, long timestamp_ns, UnityEngine.Vector3 angVel, UnityEngine.Vector3 accel, UnityEngine.Quaternion rot)
    {
        ingest_imu(id, timestamp_ns, angVel.x, angVel.y, angVel.z, accel.x, accel.y, accel.z, rot.w, rot.x, rot.y, rot.z);
    }

    public static unsafe void StreamRGB(int id, NativeArray<byte> leftBuffer, NativeArray<byte> rightBuffer,
        long timestamp_ns, UnityEngine.Quaternion rot, UnityEngine.Vector3 accel)
    {
        IntPtr pLeft = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(leftBuffer);
        IntPtr pRight = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rightBuffer);

        stream_rgb(id, pLeft, pRight, timestamp_ns, rot.w, rot.x, rot.y, rot.z, accel.x, accel.y, accel.z);
    }

    public static SimCameraInfo[] GetVirtualCameras()
    {
        IntPtr ptr = get_virtual_camera_info(out int count);
        if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<SimCameraInfo>();

        SimCameraInfo[] result = new SimCameraInfo[count];
        int size = Marshal.SizeOf<SimCameraInfo>();
        for (int i = 0; i < count; i++)
        {
            IntPtr item = new IntPtr(ptr.ToInt64() + i * size);
            result[i] = Marshal.PtrToStructure<SimCameraInfo>(item);
        }
        return result;
    }
}