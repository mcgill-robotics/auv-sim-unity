using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Interactive orbit camera that follows a target (AUV) while allowing 
/// the user to rotate with Right-Click and zoom with Scroll Wheel.
/// </summary>
public class OrbitFollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The object this camera should follow - leave empty to use SimulationSettings.AUVTransform")]
    [SerializeField] private Transform targetOverride;
    
    /// <summary>Returns the target Transform from override or SimulationSettings.</summary>
    private Transform Target => targetOverride != null ? targetOverride : SimulationSettings.Instance?.AUVTransform;
    
    [Header("Distance Settings")]
    [Tooltip("Default distance from the target")]
    public float distance = 3.0f;
    [Tooltip("Minimum distance to zoom in")]
    public float minDistance = 2.0f;
    [Tooltip("Maximum distance to zoom out")]
    public float maxDistance = 30.0f;
    [Tooltip("Speed of zooming")]
    public float zoomSpeed = 5.0f;
    
    [Header("Rotation Settings")]
    [Tooltip("Speed of horizontal rotation (degrees per unit of mouse move)")]
    public float xSpeed = 200.0f;
    [Tooltip("Speed of vertical rotation")]
    public float ySpeed = 120.0f;
    
    [Header("Pitch Limits")]
    [Tooltip("Lowest angle (looking up)")]
    public float yMinLimit = -20f;
    [Tooltip("Highest angle (looking down)")]
    public float yMaxLimit = 80f;
    
    [Header("Initial State")]
    [Tooltip("Starting horizontal angle")]
    public float initialYaw = 0f;
    [Tooltip("Starting vertical angle (looking down)")]
    public float initialPitch = 20f;
    [Tooltip("If true, resets to initial rotation every time the camera is enabled")]
    public bool resetOnEnable = false;

    [Header("Smoothing")]
    [Tooltip("How smoothly the camera position updates")]
    public float movementSmoothing = 10f;
    [Tooltip("How smoothly the camera rotation updates")]
    public float rotationSmoothing = 10f;

    [Header("Collision")]
    [Tooltip("Layers that obstruct the camera view")]
    public LayerMask collisionLayerMask = 1; // Default
    [Tooltip("Offset from the wall to prevent clipping")]
    public float collisionBuffer = 0.2f;

    private float x = 0.0f;
    private float y = 0.0f;
    private float currentDistance;
    private Quaternion currentRotation;
    private Vector3 currentPosition;
    private bool initialized = false;

    private void OnEnable()
    {
        if (Target == null) return;
        
        if (!initialized || resetOnEnable)
        {
            x = initialYaw;
            y = initialPitch;
            initialized = true;
        }
        else
        {
            // Calculate current relative offset so the camera doesn't jump
            // Relative Rotation = Inv(Target) * Current
            Quaternion relativeRot = Quaternion.Inverse(Target.rotation) * transform.rotation;
            Vector3 angles = relativeRot.eulerAngles;
            
            // Normalize angles to -180, 180 range for clamping logic
            x = (angles.y > 180) ? angles.y - 360 : angles.y;
            y = (angles.x > 180) ? angles.x - 360 : angles.x;
        }
        
        currentRotation = transform.rotation;
        currentPosition = transform.position;
        currentDistance = Vector3.Distance(transform.position, Target.position);
    }

    private void Start()
    {
        if (Target == null)
        {
            Debug.LogWarning("[OrbitFollowCamera] No target assigned!");
        }
    }

    private void LateUpdate()
    {
        if (Target == null) return;

        // 1. Handle Input (if not over UI)
        if (!Utils.UIUtils.IsMouseOverUI())
        {
            // Rotation: Right click drag (this now adds offset/delta)
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }

            // Zoom: Scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                // Multiply by a smaller factor or use a more continuous approach if needed.
                // The reason for "integer intervals" is likely that scroll delta (e.g. 2.0) * zoomSpeed (0.5) = 1.0.
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            }
        }

        // 2. Clamp Pitch (Y)
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // 3. Calculate Target Rotation
        Quaternion orbitalRotation = Quaternion.Euler(y, x, 0);
        Quaternion targetRotation = Target.rotation * orbitalRotation;
        
        // 4. Calculate Desired Distance (Collision Check)
        // Desired position without collision
        Vector3 direction = targetRotation * new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 idealPosition = Target.position + direction * distance;
        
        float targetDist = distance;
        
        // Raycast from target to ideal position to check for blockers
        RaycastHit hit;
        if (Physics.Linecast(Target.position, idealPosition, out hit, collisionLayerMask))
        {
            float hitDist = Vector3.Distance(Target.position, hit.point);
            // Clamp distance to avoid clipping, but respect minDistance
            targetDist = Mathf.Clamp(hitDist - collisionBuffer, minDistance, distance);
        }

        // 5. Smooth State Transitions
        float dt = Time.deltaTime;
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, dt * rotationSmoothing);
        
        // Use the collision-aware targetDist, but smooth it
        currentDistance = Mathf.Lerp(currentDistance, targetDist, dt * movementSmoothing);
        
        // Re-calculate smoothed position
        currentPosition = Target.position + currentRotation * new Vector3(0.0f, 0.0f, -currentDistance);

        // 6. Apply to transform
        transform.rotation = currentRotation;
        transform.position = currentPosition;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
