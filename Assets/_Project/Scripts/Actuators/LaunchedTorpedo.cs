using UnityEngine;

namespace Actuators
{
    /// <summary>
    /// Handles the kinematic trajectory of a launched torpedo using parameterized equations.
    /// </summary>
    public class LaunchedTorpedo : MonoBehaviour
    {
        [System.Serializable]
        public struct Polynomial
        {
            [Tooltip("Constant term (c0)")]
            public float c0;
            [Tooltip("Linear term (c1 * t)")]
            public float c1;
            [Tooltip("Quadratic term (c2 * t^2)")]
            public float c2;
            [Tooltip("Cubic term (c3 * t^3)")]
            public float c3;
            [Tooltip("Quartic term (c4 * t^4)")]
            public float c4;

            public float Evaluate(float t)
            {
                return c0 + (c1 * t) + (c2 * t * t) + (c3 * t * t * t) + (c4 * t * t * t * t);
            }

            public float EvaluateDerivative(float t)
            {
                return c1 + (2 * c2 * t) + (3 * c3 * t * t) + (4 * c4 * t * t * t);
            }
        }

        [System.Serializable]
        public struct TrajectoryData
        {
            public Polynomial forward;  // Z axis in local space
            public Polynomial vertical; // Y axis in local space
            public Polynomial lateral;  // X axis in local space

            [Tooltip("Time at which the recording ended. After this time, the torpedo will maintain constant velocity.")]
            public float maxTrajectoryTime;

            [Header("Protection & Interaction")]
            [Tooltip("If true, the torpedo will stop if it reaches the water surface.")]
            public bool stopAtWaterSurface;
            [Tooltip("World Y coordinate of the water surface.")]
            public float waterSurfaceY;

            [Tooltip("If true, the torpedo will stop moving when it hits another collider.")]
            public bool stopOnCollision;
        }

        private TrajectoryData trajectory;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private float startTime;
        private bool isInitialized = false;
        private bool hasStopped = false;

        private Collider[] childColliders;
        private bool[] originalTriggerStates;

        // Cache for constant velocity phase
        private Vector3 posAtMaxTime;
        private Vector3 velAtMaxTime;

        public void Initialize(TrajectoryData data)
        {
            trajectory = data;
            startPosition = transform.position;
            startRotation = transform.rotation;
            startTime = Time.time;
            hasStopped = false;

            // Find and prepare colliders
            childColliders = GetComponentsInChildren<Collider>();
            originalTriggerStates = new bool[childColliders.Length];
            for (int i = 0; i < childColliders.Length; i++)
            {
                originalTriggerStates[i] = childColliders[i].isTrigger;
                if (trajectory.stopOnCollision)
                {
                    childColliders[i].isTrigger = true;
                }
            }

            // Pre-calculate state at max time if applicable
            if (trajectory.maxTrajectoryTime > 0)
            {
                float t = trajectory.maxTrajectoryTime;
                Vector3 localPos = new Vector3(
                    trajectory.lateral.Evaluate(t),
                    trajectory.vertical.Evaluate(t),
                    trajectory.forward.Evaluate(t)
                );
                posAtMaxTime = startPosition + (startRotation * localPos);

                Vector3 localVel = new Vector3(
                    trajectory.lateral.EvaluateDerivative(t),
                    trajectory.vertical.EvaluateDerivative(t),
                    trajectory.forward.EvaluateDerivative(t)
                );
                velAtMaxTime = startRotation * localVel;
            }

            isInitialized = true;
            
            // Ensure we are kinematic to follow the trajectory exactly
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        private void Update()
        {
            if (!isInitialized || hasStopped) return;

            // Water surface protection
            if (trajectory.stopAtWaterSurface && transform.position.y >= trajectory.waterSurfaceY)
            {
                StopTorpedo("Water surface reached");
                return;
            }

            float t = Time.time - startTime;

            if (trajectory.maxTrajectoryTime > 0 && t > trajectory.maxTrajectoryTime)
            {
                // Constant velocity phase
                float overtime = t - trajectory.maxTrajectoryTime;
                transform.position = posAtMaxTime + (velAtMaxTime * overtime);
                
                if (velAtMaxTime.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(velAtMaxTime.normalized);
                }
            }
            else
            {
                // Polynomial trajectory phase
                Vector3 localDisplacement = new Vector3(
                    trajectory.lateral.Evaluate(t),
                    trajectory.vertical.Evaluate(t),
                    trajectory.forward.Evaluate(t)
                );

                transform.position = startPosition + (startRotation * localDisplacement);
                UpdateOrientation(t);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (trajectory.stopOnCollision && isInitialized && !hasStopped)
            {
                // Ignore other torpedos or launcher parts if needed, but for now stop on anything
                StopTorpedo($"Collision with {other.gameObject.name}");
            }
        }

        private void StopTorpedo(string reason)
        {
            hasStopped = true;
            Debug.Log($"[LaunchedTorpedo] {reason}. Trajectory stopped.");
            
            // Restore trigger states
            if (childColliders != null)
            {
                for (int i = 0; i < childColliders.Length; i++)
                {
                    if (childColliders[i] != null)
                        childColliders[i].isTrigger = originalTriggerStates[i];
                }
            }

            // Optionally: enable gravity and non-kinematic to let it fall/bounce
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false; 
            }
        }

        private void UpdateOrientation(float t)
        {
            Vector3 localVelocity = new Vector3(
                trajectory.lateral.EvaluateDerivative(t),
                trajectory.vertical.EvaluateDerivative(t),
                trajectory.forward.EvaluateDerivative(t)
            );

            if (localVelocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = startRotation * Quaternion.LookRotation(localVelocity.normalized);
                transform.rotation = targetRot;
            }
        }
    }
}
