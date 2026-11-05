using System;
using UnityEngine;

/// <summary>
/// Singleton that toggles between free-roam and follow cameras (C key by default).
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("User Cameras")]
    [Tooltip("Free roam camera GameObject (toggle with C key)")]
    public GameObject freeCam;

    [Tooltip("Follow camera GameObject that tracks the AUV (toggle with C key)")]
    public GameObject followCam;

    /// <summary>Raised whenever the active camera changes; the argument is true for the free camera.</summary>
    public event Action<bool> FreeCameraChanged;

    /// <summary>True while the free-roam camera is the active one.</summary>
    public bool IsFreeCameraActive => freeCam != null && freeCam.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Only remove this component: the GameObject also carries the settings components.
            Debug.LogWarning("[CameraManager] A second CameraManager was found; removing the duplicate component.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ActivateFollowCam();
    }

    private void Update()
    {
        KeyCode toggleKey = InputManager.Instance != null
            ? InputManager.Instance.GetKey(InputBindings.CameraToggle, KeyCode.C)
            : KeyCode.C;
        if (InputGate.GetKeyDown(toggleKey))
        {
            ToggleCamera();
        }
    }

    public void ToggleCamera()
    {
        if (followCam != null && followCam.activeSelf)
        {
            ActivateFreeCam();
        }
        else
        {
            ActivateFollowCam();
        }
    }

    public void ActivateFreeCam() => SetActiveCamera(freeCam);
    public void ActivateFollowCam() => SetActiveCamera(followCam);

    private void SetActiveCamera(GameObject activeCam)
    {
        if (freeCam) freeCam.SetActive(freeCam == activeCam);
        if (followCam) followCam.SetActive(followCam == activeCam);
        FreeCameraChanged?.Invoke(IsFreeCameraActive);
    }
}
