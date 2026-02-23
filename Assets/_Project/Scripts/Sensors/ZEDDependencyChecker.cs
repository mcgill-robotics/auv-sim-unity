using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ZEDDependencyChecker
{
  public bool verbose;
  // Necessary Win32 imports to check for DLLs without crashing the app
  [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
  private static extern IntPtr LoadLibrary(string lpFileName);

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool FreeLibrary(IntPtr hModule);

  // same for Linux
  [DllImport("libdl.so.2")]
  private static extern IntPtr dlopen(string fileName, int flags);
  [DllImport("libdl.so.2")]
  private static extern int dlclose(IntPtr handle);

  private static string[] DLLCriticalDeps = {
            "nvcuda.dll",           // Core CUDA
            "nvEncodeAPI64.dll",    // Needed for H264/H265 streaming
            "nvcuvid.dll",          // Video Decoding
            "MSVCP140.dll",         // VS Runtime
            "VCRUNTIME140.dll",     // VS Runtime
            "VCOMP140.DLL",         // OpenMP (Commonly missing!)
            "mfc140.dll",           // Microsoft Foundation Classes
            "CONCRT140.dll",         // Concrt Runtime
            "urlmon.dll",
            "WINMM.dll",
            "SETUPAPI.dll",
            "KERNEL32.dll",
            "USER32.dll",
            "SHELL32.dll",
            "ole32.dll",
            "OLEAUT32.dll",
            "ADVAPI32.dll",
            "WS2_32.dll",
            "PSAPI.DLL",
            "WINHTTP.dll",
            "IPHLPAPI.DLL",
            "VCRUNTIME140_1.dll",
            "api-ms-win-crt-runtime-l1-1-0.dll",
            "api-ms-win-crt-heap-l1-1-0.dll",
            "api-ms-win-crt-math-l1-1-0.dll",
            "api-ms-win-crt-stdio-l1-1-0.dll",
            "api-ms-win-crt-environment-l1-1-0.dll",
            "api-ms-win-crt-string-l1-1-0.dll",
            "api-ms-win-crt-convert-l1-1-0.dll",
            "api-ms-win-crt-filesystem-l1-1-0.dll",
            "api-ms-win-crt-locale-l1-1-0.dll",
            "api-ms-win-crt-utility-l1-1-0.dll",
            "api-ms-win-crt-time-l1-1-0.dll"
        };

  private static string[] SOCriticalDeps = {
          "libcuda.so.1",
          "libpng16.so.16",
          "libjpeg.so.8",
          "libturbojpeg.so.0",
          "libusb-1.0.so.0",
          "libnvcuvid.so.1",
          "libnvidia-encode.so.1",
          "libstdc++.so.6",
          "libm.so.6",
          "libgomp.so.1",
          "libgcc_s.so.1",
          "libc.so.6",
          "ld-linux-x86-64.so.2"
        };

  public ZEDDependencyChecker(bool verbose = false)
  {
    this.verbose = verbose;
  }
  public bool CheckDependencies(OperatingSystemFamily OS)
  {
    string[] criticalDeps;
    if (OS == OperatingSystemFamily.Windows)
    {
      criticalDeps = DLLCriticalDeps;
    }
    else if (OS == OperatingSystemFamily.Linux)
    {
      criticalDeps = SOCriticalDeps;
    }
    else
    {
      Debug.LogError("Unsupported OS for ZED SDK. Only Windows and Linux are supported.");
      return false;
    }
    if (verbose)
    {
      Debug.Log($"<color=cyan><b>--- CHECKING ZED SDK DEPENDENCIES ---</b></color>");
      Debug.Log($"Operating System: {OS}");
      Debug.Log("LD_LIBRARY_PATH=" + Environment.GetEnvironmentVariable("LD_LIBRARY_PATH"));
    }

    // 1. Check for NVIDIA Driver / CUDA
    if (!CheckNvidiaStack()) return false;


    foreach (string dep in criticalDeps)
    {
      if (OS == OperatingSystemFamily.Windows)
      {
        if (!CheckWindowsDependency(dep))
        {
          Debug.LogError($"CRITICAL: Missing dependency: {dep}");
          return false;
        }
      }
      else if (OS == OperatingSystemFamily.Linux)
      {
        if (!CheckLinuxDependency(dep))
        {
          Debug.LogError($"CRITICAL: Missing dependency: {dep}");
          return false;
        }
      }
    }

    if (verbose) Debug.Log("<color=cyan><b>--- SCAN COMPLETE ---</b></color>");
    return true;
  }

  bool CheckNvidiaStack()
  {
    // Simple check to see if we are even on an NVIDIA GPU
    string gpuName = SystemInfo.graphicsDeviceName.ToLower();
    if (verbose)
    {
      Debug.Log($"Detected GPU: {gpuName}");
      Debug.Log($"Graphics Device Vendor: {SystemInfo.graphicsDeviceVendor}");
      Debug.Log($"SystemInfo.graphicsDeviceName: {SystemInfo.graphicsDeviceName}");
    }
    if (!gpuName.Contains("nvidia"))
    {
      Debug.LogError("CRITICAL: No NVIDIA GPU detected. ZED SDK requires NVIDIA hardware on Windows.");
      return false;
    }
    return true;
  }

  bool CheckWindowsDependency(string dllName)
  {
    try
    {
      IntPtr handle = LoadLibrary(dllName);
      if (handle == IntPtr.Zero)
      {
        int errorCode = Marshal.GetLastWin32Error();
        if (verbose) Debug.LogWarning($"Failed to load {dllName}. Error code: {errorCode}");
        return false;
      }
      FreeLibrary(handle);
    }
    catch (Exception ex)
    {
      if (verbose) Debug.LogWarning($"Exception while loading {dllName}: {ex.Message}");
      return false;
    }

    if (verbose) Debug.Log($"Found dependency: {dllName}");
    return true;
  }
  bool CheckLinuxDependency(string soName)
  {
    const int RTLD_NOW = 2;
    try
    {
      IntPtr handle = dlopen(soName, RTLD_NOW);
      if (handle == IntPtr.Zero)
      {
        if (verbose) Debug.LogWarning($"Failed to load {soName}. It may be missing or not in the library path.");
        return false;
      }
      dlclose(handle);
    }
    catch (Exception ex)
    {
      if (verbose) Debug.LogWarning($"Exception while loading {soName}: {ex.Message}");
      return false;
    }

    if (verbose) Debug.Log($"Found dependency: {soName}");
    return true;
  }
}