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

  string[] DLLCriticalDeps = {
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

  string[] SOCriticalDeps = {

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
    }

    // 1. Check for NVIDIA Driver / CUDA
    if (!CheckNvidiaStack()) return false;


    foreach (string dll in criticalDeps)
    {
      if (verbose) Debug.Log($"Checking for {dll}...");
      IntPtr handle = LoadLibrary(dll);
      if (handle != IntPtr.Zero)
      {
        if (verbose) Debug.Log($"<color=green>[FOUND]</color> {dll}");
        FreeLibrary(handle); // Clean up
      }
      else
      {
        // This will tell us exactly which file is missing
        Debug.LogError($"<color=red>[MISSING]</color> {dll}. This will cause init_streamer to return -1.");
        return false;
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
    }
    if (!gpuName.Contains("nvidia"))
    {
      Debug.LogError("CRITICAL: No NVIDIA GPU detected. ZED SDK requires NVIDIA hardware on Windows.");
      return false;
    }
    return true;
  }
}