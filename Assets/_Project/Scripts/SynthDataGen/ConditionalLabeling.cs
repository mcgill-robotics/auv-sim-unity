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

    [Tooltip("If true, allows labeling when viewing the front face.")]
    public bool allowFrontFace = true;

    [Tooltip("If true, allows labeling when viewing the back face.")]
    public bool allowBackFace = true;

    private Labeling _labeling;
    private float _maxDistanceSqr;
    private float _minViewDot;

    void Awake()
    {
        _labeling = GetComponent<Labeling>();
        RecalculateThresholds();
    }

    void Start()
    {
        if (_labeling == null)
            _labeling = GetComponent<Labeling>();

        if (perceptionCamera == null && Camera.main != null)
            perceptionCamera = Camera.main.transform;
    }

    void OnValidate()
    {
        RecalculateThresholds();
    }

    void Update()
    {
        bool shouldLabel = ShouldLabel();
        if (_labeling.enabled != shouldLabel)
            _labeling.enabled = shouldLabel;
    }

    public bool ShouldLabel()
    {
        if (perceptionCamera == null || _labeling == null) return false;


        // 1. Distance Check
        Vector3 cameraToObj = transform.position - perceptionCamera.position;
        float sqrDistance = cameraToObj.sqrMagnitude;
        if (useDistanceFilter)
        {
            if (sqrDistance > _maxDistanceSqr)
                return false; // too far, no need to check angle
        }

        // 2. Angle Check (only if still passing)
        if (useAngleFilter)
        {
            if (sqrDistance <= Mathf.Epsilon)
                return true;

            float inverseDistance = 1f / Mathf.Sqrt(sqrDistance);
            float viewDot = Vector3.Dot(transform.forward, -cameraToObj * inverseDistance);
            bool isValidAngle = (allowFrontFace && viewDot >= _minViewDot) ||
                (allowBackFace && viewDot <= -_minViewDot);

            if (!isValidAngle)
                return false;
        }
        return true;
    }

    private void RecalculateThresholds()
    {
        _maxDistanceSqr = maxDistance * maxDistance;
        _minViewDot = Mathf.Cos(maxViewAngle * Mathf.Deg2Rad);
    }
}
