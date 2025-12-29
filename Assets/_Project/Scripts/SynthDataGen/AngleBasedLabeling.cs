using UnityEngine;
using UnityEngine.Perception.GroundTruth.LabelManagement;

[RequireComponent(typeof(Labeling))]
public class AngleBasedLabeling : MonoBehaviour
{
    [Tooltip("The main camera used for perception")]
    public Transform perceptionCamera;

    [Tooltip("How close to 'head-on' must the view be? 0=Strictly Front, 90=Any Angle.")]
    [Range(0, 90)]
    public float maxViewAngle = 60f;

    [Tooltip("If true, allows seeing the back of the object too.")]
    public bool allowBackFace = true;

    private Labeling _labeling;

    void Start()
    {
        _labeling = GetComponent<Labeling>();
        
        // Auto-find camera if not assigned
        if (perceptionCamera == null)
        {
            if (Camera.main != null)
                perceptionCamera = Camera.main.transform;
            else
                Debug.LogError("AngleBasedLabeling: No Camera found!");
        }
    }

    void Update()
    {
        if (perceptionCamera == null) return;

        // 1. Calculate vector from Camera to Object
        Vector3 cameraToObj = transform.position - perceptionCamera.position;
        
        // 2. Calculate Angle between Object's Forward vector and the Camera look vector
        // We use the object's 'Forward' (Z+) as the face of the gate.
        float angle = Vector3.Angle(transform.forward, -cameraToObj);

        // 3. Logic: 
        // 0 degrees = Looking straight at the front face
        // 90 degrees = Looking at the side
        // 180 degrees = Looking at the back face
        
        bool isValidAngle = false;

        if (angle <= maxViewAngle)
        {
            // Front face visible
            isValidAngle = true;
        }
        else if (allowBackFace && angle >= (180f - maxViewAngle))
        {
            // Back face visible
            isValidAngle = true;
        }

        // 4. Toggle the Labeling Component
        // If disabled, the Perception package will ignore this object this frame
        if (_labeling.enabled != isValidAngle)
        {
            _labeling.enabled = isValidAngle;
        }
    }
}