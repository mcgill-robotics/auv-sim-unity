using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The object this camera should follow (usually the AUV)")]
    public GameObject targetObject;
    
    [Header("Follow Configuration")]
    [Tooltip("Distance behind the target")]
    public float followDistance = 3.0f;
    
    [Tooltip("Height above the target")]
    public float height = 1.5f;
    
    [Tooltip("How much to look down at the target (in degrees)")]
    public float pitchAngle = 15f;
    
    [Tooltip("How tightly the camera follows. Higher = stiffer, Lower = smoother.")]
    public float rigidity = 5f;

    private void Start()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[CameraFollower] Target Object not assigned! Disabling script.");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (targetObject == null) return;

        // Calculate the desired position relative to the target's rotation
        // We want to be behind (-forward) and above (+up)
        Vector3 targetPos = targetObject.transform.position;
        Vector3 forward = targetObject.transform.forward;
        Vector3 up = targetObject.transform.up;

        // Target position is: Target - (Forward * Distance) + (Up * Height)
        Vector3 desiredPosition = targetPos - (forward * followDistance) + (up * height);

        // Smoothly move to desired position
        float dt = Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, dt * rigidity);

        // Calculate rotation: Look at the target, then apply pitch offset
        Quaternion lookRotation = Quaternion.LookRotation(targetPos - transform.position, up);
        // Or simpler: just match target yaw and apply pitch
        // But LookRotation is better because it keeps the target centered
        
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, dt * rigidity);
    }
}