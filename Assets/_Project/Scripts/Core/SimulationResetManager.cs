using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace Core
{
    /// <summary>
    /// Manages automated Software-In-The-Loop (SITL) simulation resets for ROS 2 PID tuning
    /// pipelines (e.g., CMA-ES or Reinforcement Learning).
    /// 
    /// FEATURES:
    /// 1. ROS-TCP Trigger: Listens to /simulation/reset (std_msgs/Bool) to trigger resets in FixedUpdate.
    /// 2. Kinematic State Reset: Zeroes all linear/angular momentum and snaps AUV back to start pose.
    /// 3. Domain Randomization: Randomizes Rigidbody mass (+-5%) and hydrodynamic drag coefficients (+-15%) on every run.
    /// 4. Fast-Forward Time & Clock Sync: Manages Time.timeScale and synchronizes ROS /clock publishing rate.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SimulationResetManager : MonoBehaviour
    {
        public static SimulationResetManager Instance { get; private set; }

        [Header("AUV References")]
        [Tooltip("Reference to the AUV Rigidbody. If null, falls back to SimulationSettings.Instance.AUVRigidbody.")]
        public Rigidbody auvRigidbody;

        [Tooltip("Reference to the HydrodynamicDrag script. If null, attempts GetComponent on the AUV Rigidbody.")]
        public HydrodynamicDrag auvDrag;

        [Tooltip("Reference to the Buoyancy script. If null, attempts GetComponent on the AUV Rigidbody.")]
        public Buoyancy auvBuoyancy;

        [Tooltip("Reference to the Thrusters script. If null, attempts GetComponent on the AUV Rigidbody.")]
        public Thrusters auvThrusters;

        [Header("Sensor References")]
        [Tooltip("Reference to the DVLPublisher script. If null, attempts GetComponentInChildren on the AUV Rigidbody.")]
        public DVLPublisher auvDvl;

        [Tooltip("Reference to the IMUPublisher script. If null, attempts GetComponentInChildren on the AUV Rigidbody.")]
        public IMUPublisher auvImu;

        [Header("Reset Behavior")]
        [Tooltip("If true, resets the AUV to (0, -1.5, 0) and Quaternion.identity. If false, resets to its initial start pose.")]
        public bool resetToOrigin = false;

        [Tooltip("If true, applies domain randomization to mass and drag on every reset.")]
        public bool enableDomainRandomization = true;

        [Header("Domain Randomization Bounds")]
        [Tooltip("Percentage variation for Rigidbody mass (e.g., 0.05 for +-5%).")]
        [Range(0f, 0.5f)]
        public float massRandomizationRange = 0.05f;

        [Tooltip("Percentage variation for Hydrodynamic Drag coefficients (e.g., 0.15 for +-15%).")]
        [Range(0f, 0.5f)]
        public float dragRandomizationRange = 0.10f;

        [Tooltip("Percentage variation for Buoyancy Force (e.g., 0.02 for +-2% salinity/temperature variation).")]
        [Range(0f, 0.1f)]
        public float buoyancyRandomizationRange = 0.02f;

        [Tooltip("Maximum Center of Mass offset shift in meters (e.g., 0.002 for +-2mm internal payload shift).")]
        [Range(0f, 0.02f)]
        public float comOffsetRange = 0.002f;

        [Tooltip("Per-thruster efficiency variance applied on reset (e.g., 0.10 means each of the 8 thrusters gets +-10% random asymmetric thrust loss).")]
        [Range(0f, 0.3f)]
        public float thrusterEfficiencyVarianceOnReset = 0.05f;

        [Tooltip("Percentage variation for global Thruster force multiplier (e.g., 0.05 for +-5% battery voltage sag).")]
        [Range(0f, 0.2f)]
        public float forceMultiplierRandomizationRange = 0.05f;

        [Tooltip("If true, injects random water current drift (ocean disturbance) on every reset.")]
        public bool randomizeWaterCurrent = true;

        [Tooltip("Maximum velocity of random water current disturbance in m/s (e.g., 0.1 for up to 0.1 m/s drift).")]
        [Range(0f, 1f)]
        public float maxWaterCurrentSpeed = 0.1f;

        [Tooltip("If true, randomizes sensor delay buffer latency on every reset.")]
        public bool randomizeSensorDelay = true;

        [Tooltip("Minimum sensor latency delay in seconds when randomized (e.g., 0.10s for 100ms).")]
        [Range(0.05f, 0.3f)]
        public float minSensorDelay = 0.10f;

        [Tooltip("Maximum sensor latency delay in seconds when randomized (e.g., 0.20s for 200ms).")]
        [Range(0.05f, 0.3f)]
        public float maxSensorDelay = 0.20f;

        [Header("Simulation Speed & Time Sync")]
        [Tooltip("Simulation speed multiplier (e.g., 5.0x or 10.0x for fast-forward tuning).")]
        [Range(0.1f, 50f)]
        public float timeScale = 1.0f;

        [Tooltip("Target publish rate (Hz) for the ROS /clock topic. Set to 50+ Hz during fast-forward tuning so ROS controllers receive smooth dt updates.")]
        [Range(10f, 200f)]
        public float clockPublishRate = 50.0f;

        [Header("Events")]
        public UnityEvent onSimulationReset;

        // Internal nominal state storage
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float nominalMass;
        private Vector3 nominalDragCoefficients;
        private Vector3 nominalLumpedQuadraticDrag;
        private Vector3 nominalAngularQuadraticDrag;
        private Vector3 nominalAddedMassTranslational;
        private Vector3 nominalAddedMassRotational;
        private float nominalBuoyancyForce;
        private Vector3 nominalCenterOfMass;
        private float nominalEfficiencyVariance;
        private float nominalForceMultiplier;

        // ROS connection and thread-safe trigger
        private ROSConnection roscon;
        private volatile bool resetRequested = false;
        private float lastTimeScale = 1.0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 1. Resolve AUV references
            if (auvRigidbody == null && SimulationSettings.Instance != null)
            {
                auvRigidbody = SimulationSettings.Instance.AUVRigidbody;
            }

            if (auvRigidbody != null && auvDrag == null)
            {
                auvDrag = auvRigidbody.GetComponent<HydrodynamicDrag>();
            }

            if (auvRigidbody != null && auvBuoyancy == null)
            {
                auvBuoyancy = auvRigidbody.GetComponent<Buoyancy>();
            }

            if (auvRigidbody != null && auvThrusters == null)
            {
                auvThrusters = auvRigidbody.GetComponent<Thrusters>();
            }

            if (auvRigidbody != null && auvDvl == null)
            {
                auvDvl = auvRigidbody.GetComponentInChildren<DVLPublisher>(true);
            }

            if (auvRigidbody != null && auvImu == null)
            {
                auvImu = auvRigidbody.GetComponentInChildren<IMUPublisher>(true);
            }

            if (auvRigidbody == null)
            {
                Debug.LogError("[SimulationResetManager] No AUV Rigidbody assigned or found in SimulationSettings!");
                return;
            }

            // 2. Cache nominal kinematic and physical properties
            initialPosition = auvRigidbody.position;
            initialRotation = auvRigidbody.rotation;
            nominalMass = auvRigidbody.mass;

            if (auvDrag != null)
            {
                nominalDragCoefficients = auvDrag.dragCoefficients;
                nominalLumpedQuadraticDrag = auvDrag.lumpedQuadraticDrag;
                nominalAngularQuadraticDrag = auvDrag.angularQuadraticDrag;
                nominalAddedMassTranslational = auvDrag.addedMassTranslational;
                nominalAddedMassRotational = auvDrag.addedMassRotational;
            }

            if (auvBuoyancy != null)
            {
                nominalBuoyancyForce = auvBuoyancy.manualBuoyancyForce;
                nominalCenterOfMass = auvBuoyancy.centerOfMass;
            }

            if (auvThrusters != null)
            {
                nominalEfficiencyVariance = auvThrusters.efficiencyVariance;
                nominalForceMultiplier = auvThrusters.forceMultiplier;
            }

            // 3. Configure TimeScale & ROS Clock Synchronization
            ApplyTimeScale(timeScale);
            ConfigureROSClock();

            // 4. Subscribe to ROS Reset Topic
            roscon = ROSConnection.GetOrCreateInstance();
            string resetTopic = ROSSettings.Instance != null && !string.IsNullOrEmpty(ROSSettings.Instance.SimulationResetTopic)
                ? ROSSettings.Instance.SimulationResetTopic
                : "/simulation/reset";

            roscon.Subscribe<BoolMsg>(resetTopic, OnRosResetReceived);
            Debug.Log($"[SimulationResetManager] Subscribed to ROS reset trigger on '{resetTopic}'. Ready for SITL tuning.");
        }

        private void OnValidate()
        {
            if (Application.isPlaying && !Mathf.Approximately(timeScale, lastTimeScale))
            {
                ApplyTimeScale(timeScale);
            }
        }

        private void ApplyTimeScale(float newTimeScale)
        {
            timeScale = Mathf.Clamp(newTimeScale, 0.1f, 100f);
            Time.timeScale = timeScale;
            lastTimeScale = timeScale;
            Debug.Log($"[SimulationResetManager] Time.timeScale updated to {timeScale}x.");
        }

        private void ConfigureROSClock()
        {
            ROSClock rosClock = FindFirstObjectByType<ROSClock>();
            if (rosClock != null)
            {
                rosClock.SetPublishRate(clockPublishRate);
                Debug.Log($"[SimulationResetManager] ROSClock publish rate synchronized to {clockPublishRate} Hz.");
            }
            else
            {
                Debug.LogWarning("[SimulationResetManager] No ROSClock found in scene. ROS /clock publishing may not be synchronized!");
            }
        }

        private void OnRosResetReceived(BoolMsg msg)
        {
            // Trigger reset on next FixedUpdate cycle (thread-safe for PhysX)
            resetRequested = true;
        }

        private void FixedUpdate()
        {
            if (resetRequested)
            {
                resetRequested = false;
                PerformReset();
            }
        }

        /// <summary>
        /// Instantly resets AUV kinematic state, applies domain randomization, and fires reset events.
        /// Can be invoked manually or via UnityInspector context menu.
        /// </summary>
        [ContextMenu("Trigger Simulation Reset")]
        public void TriggerReset()
        {
            resetRequested = true;
        }

        private void PerformReset()
        {
            if (auvRigidbody == null) return;

            // 1. Kinematic Pose Reset - uses (0, -1.5, 0) if resetToOrigin is true to avoid surface/wave physics, otherwise returns to initial start pose
            Vector3 targetPos = resetToOrigin ? new Vector3(0f, -1.5f, 0f) : initialPosition;
            Quaternion targetRot = resetToOrigin ? Quaternion.identity : initialRotation;

            auvRigidbody.position = targetPos;
            auvRigidbody.rotation = targetRot;
            auvRigidbody.transform.position = targetPos;
            auvRigidbody.transform.rotation = targetRot;

            // 2. Kill all residual momentum
            auvRigidbody.linearVelocity = Vector3.zero;
            auvRigidbody.angularVelocity = Vector3.zero;

            // Clear Hydrodynamics acceleration history to prevent artificial added-mass spikes
            if (auvDrag != null)
            {
                auvDrag.ResetState();
            }

            // Clear Sensor integration and bias state (e.g. DVL odometry accumulators and IMU velocity history)
            if (auvDvl != null)
            {
                auvDvl.ResetState();
            }
            if (auvImu != null)
            {
                auvImu.ResetState();
            }
            if (auvThrusters != null)
            {
                auvThrusters.ResetState();
            }
            SensorDelayBuffer delayBuf = auvRigidbody.GetComponent<SensorDelayBuffer>();
            if (delayBuf != null)
            {
                delayBuf.ResetState();
            }

            // 3. Domain Randomization (Sim-to-Real regularization)
            if (enableDomainRandomization)
            {
                ApplyDomainRandomization();
            }

            // 4. Fire callback events
            onSimulationReset?.Invoke();

            Debug.Log($"[SimulationResetManager] Simulation reset complete. Mass: {auvRigidbody.mass:F2} kg | " +
                      (auvDrag != null ? $"Cd: {auvDrag.dragCoefficients.x:F2}, {auvDrag.dragCoefficients.y:F2}, {auvDrag.dragCoefficients.z:F2} | Current Drift: {auvDrag.waterCurrent.magnitude:F2} m/s" : ""));
        }

        private void ApplyDomainRandomization()
        {
            // Randomize Mass by +-5% (or configured range)
            float massFactor = UnityEngine.Random.Range(1f - massRandomizationRange, 1f + massRandomizationRange);
            auvRigidbody.mass = nominalMass * massFactor;

            // Randomize Drag Coefficients by +-15% (or configured range)
            if (auvDrag != null)
            {
                Vector3 dragFactor = new Vector3(
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange),
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange),
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange)
                );

                auvDrag.dragCoefficients = Vector3.Scale(nominalDragCoefficients, dragFactor);
                auvDrag.lumpedQuadraticDrag = Vector3.Scale(nominalLumpedQuadraticDrag, dragFactor);

                // Randomize Angular Drag across Roll/Pitch/Yaw by +-15% (crucial for Phase 1 Attitude tuning!)
                Vector3 angDragFactor = new Vector3(
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange),
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange),
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange)
                );
                auvDrag.angularQuadraticDrag = Vector3.Scale(nominalAngularQuadraticDrag, angDragFactor);

                // Randomize Added Mass (+-15%) so effective inertia is varied
                Vector3 addedMassFactor = new Vector3(
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange),
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange),
                    UnityEngine.Random.Range(1f - dragRandomizationRange, 1f + dragRandomizationRange)
                );
                auvDrag.addedMassTranslational = Vector3.Scale(nominalAddedMassTranslational, addedMassFactor);
                auvDrag.addedMassRotational = Vector3.Scale(nominalAddedMassRotational, addedMassFactor);

                // Randomize Water Current disturbance (ocean drift)
                if (randomizeWaterCurrent && maxWaterCurrentSpeed > 0f)
                {
                    Vector2 randomDir = UnityEngine.Random.insideUnitCircle * maxWaterCurrentSpeed;
                    auvDrag.waterCurrent = new Vector3(randomDir.x, 0f, randomDir.y);
                }
                else
                {
                    auvDrag.waterCurrent = Vector3.zero;
                }
            }

            // Randomize Buoyancy Force (+-2% for salinity/temp) and CoM offset (+-2mm for internal payload shift)
            if (auvBuoyancy != null)
            {
                float buoyancyFactor = UnityEngine.Random.Range(1f - buoyancyRandomizationRange, 1f + buoyancyRandomizationRange);
                auvBuoyancy.manualBuoyancyForce = nominalBuoyancyForce * buoyancyFactor;

                Vector3 comOffset = new Vector3(
                    UnityEngine.Random.Range(-comOffsetRange, comOffsetRange),
                    UnityEngine.Random.Range(-comOffsetRange, comOffsetRange),
                    UnityEngine.Random.Range(-comOffsetRange, comOffsetRange)
                );
                auvBuoyancy.centerOfMass = nominalCenterOfMass + comOffset;
                auvRigidbody.centerOfMass = auvBuoyancy.centerOfMass;
            }

            // Randomize per-thruster efficiency asymmetry and global force multiplier
            if (auvThrusters != null)
            {
                // Per-thruster asymmetry (each thruster gets a different random scalar)
                auvThrusters.efficiencyVariance = thrusterEfficiencyVarianceOnReset;
                auvThrusters.InitializeEfficiency();

                // Global force multiplier (uniform battery voltage sag)
                auvThrusters.forceMultiplier = nominalForceMultiplier * UnityEngine.Random.Range(1f - forceMultiplierRandomizationRange, 1f + forceMultiplierRandomizationRange);
            }

            // Randomize Sensor Delay Buffer Latency
            if (randomizeSensorDelay)
            {
                SensorDelayBuffer delayBuf = auvRigidbody.GetComponent<SensorDelayBuffer>();
                if (delayBuf != null)
                {
                    delayBuf.delayTime = UnityEngine.Random.Range(minSensorDelay, maxSensorDelay);
                    Debug.Log($"[SimulationResetManager] Randomized sensor latency to {delayBuf.delayTime * 1000f:F1}ms.");
                }
            }
        }
    }
}
