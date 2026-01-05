using UnityEngine;
using UnityEngine.Perception.GroundTruth.LabelManagement;

/// <summary>
/// Enables/disables the Labeling component based on distance and viewing angle.
/// Labeling is only enabled if BOTH conditions are met:
/// 1. Camera is within maxDistance
/// 2. Camera is viewing from a valid angle (front or back face)
/// </summary>
[RequireComponent(typeof(Labeling))]
public class ConditionalLabeling : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("The perception camera. Auto-finds Camera.main if empty.")]
    public Transform perceptionCamera;

    [Header("Distance Filter")]
    [Tooltip("Enable distance-based filtering.")]
    public bool useDistanceFilter = true;
    
    [Tooltip("Maximum distance (meters) at which the object is labeled.")]
    public float maxDistance = 15f;

    [Header("Angle Filter")]
    [Tooltip("Enable angle-based filtering.")]
    public bool useAngleFilter = true;
    
    [Tooltip("Max angle from head-on view (0=strictly front, 90=any angle).")]
    [Range(0, 90)]
    public float maxViewAngle = 60f;
    
    [Tooltip("If true, also allows seeing the back face.")]
    public bool allowBackFace = true;

    private Labeling _labeling;

    void Start()
    {
      _labeling = GetComponent<Labeling>();
      
      if (perceptionCamera == null && Camera.main != null)
          perceptionCamera = Camera.main.transform;
    }

    void Update()
    {
      if (perceptionCamera == null || _labeling == null) return;

      bool shouldLabel = true;

      // 1. Distance Check
      if (useDistanceFilter)
      {
          float distance = Vector3.Distance(transform.position, perceptionCamera.position);
          if (distance > maxDistance)
              shouldLabel = false;
      }

      // 2. Angle Check (only if still passing)
      if (shouldLabel && useAngleFilter)
      {
          Vector3 cameraToObj = transform.position - perceptionCamera.position;
          float angle = Vector3.Angle(transform.forward, -cameraToObj);

          bool isValidAngle = angle <= maxViewAngle;
          if (!isValidAngle && allowBackFace)
              isValidAngle = angle >= (180f - maxViewAngle);

          if (!isValidAngle)
              shouldLabel = false;
      }

      // 3. Apply result
      if (_labeling.enabled != shouldLabel)
          _labeling.enabled = shouldLabel;
    }
}
