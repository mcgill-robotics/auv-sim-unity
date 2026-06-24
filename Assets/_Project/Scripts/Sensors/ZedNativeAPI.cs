using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

/// <summary>
/// A clean, strongly-typed C# wrapper for the ZED C++ Streaming API.
/// </summary>
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
  public struct StreamingParameters
  {
    public int mode;
    public float qx, qy, qz, qw;
    public float tx, ty, tz;
    public int image_width;
    public int image_height;
    public CodecType codec_type;
    public ushort port;
    public int fps;
    public int serial_number;
    [MarshalAs(UnmanagedType.I1)] public bool alpha_channel_included;
    public InputFormat input_format;
    [MarshalAs(UnmanagedType.I1)] public bool verbose;
    public TransportMode transport_layer_mode;
    public int bitrate;
    public ushort chunk_size;

    /// <summary>
    /// Creates a cleanly initialized parameter struct with sensible defaults.
    /// </summary>
    public static StreamingParameters CreateDefault(int width, int height, int fps, ushort port, int serialNum)
    {
      return new StreamingParameters
      {
        mode = 1,
        qw = 1f, // Identity rotation
        image_width = width,
        image_height = height,
        codec_type = CodecType.H264,
        port = port,
        fps = fps,
        serial_number = serialNum,
        alpha_channel_included = false, // Pure RGB24
        input_format = InputFormat.RGB,
        verbose = false,
        transport_layer_mode = TransportMode.Network,
        bitrate = 8000,
        chunk_size = 4096
      };
    }
  }

  // --- Private Raw DllImports ---

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

  // --- Public Clean Methods ---

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

  /// <summary>
  /// Safely unwraps Unity NativeArrays into C++ Pointers and sends them to the ZED SDK.
  /// </summary>
  public static unsafe void StreamRGB(int id, NativeArray<byte> leftBuffer, NativeArray<byte> rightBuffer,
      long timestamp_ns, UnityEngine.Quaternion rot, UnityEngine.Vector3 accel)
  {
    IntPtr pLeft = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(leftBuffer);
    IntPtr pRight = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rightBuffer);

    stream_rgb(id, pLeft, pRight, timestamp_ns, rot.w, rot.x, rot.y, rot.z, accel.x, accel.y, accel.z);
  }
}