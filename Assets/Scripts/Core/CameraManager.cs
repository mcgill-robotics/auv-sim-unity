using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("User Cameras")]
    [Tooltip("Free roam camera GameObject (toggle with C key)")]
    public GameObject freeCam;
    
    [Tooltip("Follow camera GameObject that tracks the AUV (toggle with C key)")]
    public GameObject followCam;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ActivateFollowCam(); // Default
    }

    private void Update()
    {
        // Toggle Camera on 'C' press
        if (Input.GetKeyDown(InputManager.Instance != null ? InputManager.Instance.GetKey("ToggleCamera", KeyCode.C) : KeyCode.C))
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
    }
}
