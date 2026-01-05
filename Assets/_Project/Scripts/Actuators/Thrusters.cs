using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Auv;
using Utils;

public class Thrusters : MonoBehaviour
{
    [Header("Manual Control Forces")]
    [Tooltip("Force (N) applied per thruster when sinking (Q key)")]
    [Range(1f, 100f)]
    public float sinkForce = 30f;
    
    [Tooltip("Force (N) applied per thruster when floating (E key)")]
    [Range(1f, 50f)]
    public float floatForce = 12f;
    
    [Tooltip("Force (N) applied per thruster for forward/backward movement (WASD)")]
    [Range(1f, 50f)]
    public float moveForce = 15f;
    
    [Tooltip("Force (N) applied per thruster for rotation (IJKL/UO)")]
    [Range(0.1f, 10f)]
    public float rotationForce = 1.0f;
    
    [Space(10)]
    [Header("Thruster Configuration")]
    [Tooltip("Array of thruster transforms (8 total: 4 horizontal, 4 vertical)")]
    public Transform[] thrusters;
    
    [Tooltip("Particle systems for each thruster (visual feedback)")]
    public ParticleSystem[] thrusterParticles;
    
    [Tooltip("Real-to-sim force scaling factor. Higher = more sensitive to ROS commands")]
    [Range(1f, 10f)]
    public float AUVRealForceMultiplier = 3;
    
    [Space(10)]
    [Header("Force Visualization")]
    [Tooltip("Show 3D arrows indicating thruster force direction and magnitude")]
    public bool enableForceVisualization = true;
    
    [Tooltip("Scale factor for arrow length (magnitude visualization)")]
    [Range(0.001f, 0.1f)]
    public float forceVisualScale = 0.01f;

    [Tooltip("Color for thruster force arrows")]
    public Color visualizationColor = Color.red;

    [Space(10)]
    [Header("Thruster Limits")]
    [Tooltip("Maximum force (N) a single thruster can output. T200 ≈ 50N, T500 ≈ 150N")]
    [Range(10f, 200f)]
    public float maxThrusterForce = 50f;

    [Space(10)]
    [Tooltip("Rate at which thruster force changes (N/s). Simulates motor inertia.")]
    public float rampRate = 100f;

    [Tooltip("Random efficiency variance (+/- percent) applied to each thruster at startup. 0.1 = +/- 10%")]
    [Range(0f, 0.1f)]
    public float efficiencyVariance = 0.05f;
    
    [Tooltip("AUV Rigidbody - leave empty to use SimulationSettings.AUVRigidbody")]
    [SerializeField] private Rigidbody auvRbOverride;
    
    /// <summary>Returns the AUV Rigidbody from override or SimulationSettings.</summary>
    private Rigidbody AuvRb => auvRbOverride != null ? auvRbOverride : SimulationSettings.Instance?.AUVRigidbody;

    private GameObject[] arrowInstances;
    private Material arrowMat;

    private ROSConnection roscon;
    private bool isFrozen = false;
    private double[] rosThrusterForces = new double[8];
    private double[] inputThrusterForces = new double[8];
    private float[] thrusterEfficiencyScalars = new float[8];
    private float[] currentThrusterLevels = new float[8];
    private float massScalarRealToSim;
    
    // Performance: State tracking to avoid redundant updates
    private bool[] particlesPlaying = new bool[8];
    private float[] previousForces = new float[8];
    private const float FORCE_UPDATE_THRESHOLD = 0.5f;  // Only update arrows if force changes by this amount
    
    // Pre-calculated force multipliers
    private float moveForceOver4, moveForceOver2, sinkForceOver4, floatForceOver4, rotationForceOver4;
    
    // Cached quality level to avoid repeated calls
    private int cachedQualityLevel;

    private void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<ThrusterForcesMsg>(ROSSettings.Instance.ThrusterForcesTopic, SetThrusterForces);
        
        massScalarRealToSim = 1f / AUVRealForceMultiplier;
        moveForceOver4 = moveForce / 4;
        moveForceOver2 = moveForce / 2;
        sinkForceOver4 = sinkForce / 4;
        floatForceOver4 = floatForce / 4;
        rotationForceOver4 = rotationForce / 4;

        InitializeEfficiency();
        InitializeArrows();
        
        // Cache quality level
        cachedQualityLevel = QualitySettings.GetQualityLevel();
    }
    
    public void UpdateQualityLevel(int newQualityLevel)
    {
        cachedQualityLevel = newQualityLevel;
    }

    private void InitializeArrows()
    {
        arrowInstances = new GameObject[thrusters.Length];
        
        // Create material and template arrow using shared utility
        arrowMat =VisualizationUtils.CreateMaterial(visualizationColor);
        GameObject templateArrow =VisualizationUtils.CreateArrow("DefaultArrow", arrowMat, 0.03f);

        for (int i = 0; i < thrusters.Length; i++)
        {
            arrowInstances[i] = Instantiate(templateArrow, thrusters[i].position, Quaternion.identity);
            arrowInstances[i].transform.parent = thrusters[i]; // Parent to thruster so it moves with AUV
           VisualizationUtils.SetXRayLayer(arrowInstances[i]);
            
            // Apply color to each instance's renderers (MaterialPropertyBlock is NOT copied by Instantiate)
            foreach (var ren in arrowInstances[i].GetComponentsInChildren<Renderer>())
            {
               VisualizationUtils.SetColorProperty(ren, visualizationColor);
            }
            
            arrowInstances[i].SetActive(false);
        }

        // Destroy the template (instances keep their own copies of the GO but share the material)
        Destroy(templateArrow);
    }



    private void Update()
    {
        HandleFreezeInput();
        HandleMovementInput();
    }
    
    private void FixedUpdate()
    {
        for (int i = 0; i < thrusters.Length; i++)
        {
            if (thrusters[i].position.y >= 0) continue;
            
            float targetForce = (float)(rosThrusterForces[i] + inputThrusterForces[i]);
            
            // Apply Ramp-up (Motor Inertia) - command signal ramps toward target
            currentThrusterLevels[i] = Mathf.MoveTowards(currentThrusterLevels[i], targetForce, rampRate * Time.fixedDeltaTime);
            float finalForce = currentThrusterLevels[i];
            
            // Apply Efficiency Variance (per-thruster manufacturing variance)
            finalForce *= thrusterEfficiencyScalars[i];
            
            // Clamp AFTER efficiency - physical thruster saturation (motor torque/RPM limit)
            finalForce = Mathf.Clamp(finalForce, -maxThrusterForce, maxThrusterForce);

            Vector3 worldForceDirection = thrusters[i].TransformDirection(Vector3.up);
            Vector3 thrusterForceVector = worldForceDirection * (finalForce * massScalarRealToSim);
            
            AuvRb.AddForceAtPosition(thrusterForceVector, thrusters[i].position, ForceMode.Force);
            
            bool shouldPlay = Math.Abs(finalForce) > 0.01f && cachedQualityLevel < 2;

            // Particles - only change state when needed
            if (shouldPlay != particlesPlaying[i])
            {
                particlesPlaying[i] = shouldPlay;
                if (shouldPlay)
                    thrusterParticles[i].Play();
                else
                    thrusterParticles[i].Stop();
            }

            // 3D Arrows - only update when force changes significantly
            if (enableForceVisualization && arrowInstances != null && i < arrowInstances.Length)
            {
                bool hasForce = Math.Abs(finalForce) > 0.01f;
                float forceDelta = Math.Abs(finalForce - previousForces[i]);
                
                if (hasForce)
                {
                    // Only update transforms if force changed significantly
                    if (forceDelta > FORCE_UPDATE_THRESHOLD || !arrowInstances[i].activeSelf)
                    {
                        arrowInstances[i].SetActive(true);
                        arrowInstances[i].transform.position = thrusters[i].position;
                        
                        if (thrusterForceVector.sqrMagnitude > 0.001f)
                        {
                            arrowInstances[i].transform.rotation = Quaternion.LookRotation(thrusterForceVector) * Quaternion.Euler(90, 0, 0);
                        }

                        float length = Math.Abs(finalForce) * forceVisualScale;
                        arrowInstances[i].transform.localScale = new Vector3(1, length, 1);
                        previousForces[i] = finalForce;
                    }
                }
                else if (arrowInstances[i].activeSelf)
                {
                    arrowInstances[i].SetActive(false);
                    previousForces[i] = 0;
                }
            }
        }
    }

    private void HandleFreezeInput()
    {
        if (Input.GetKeyDown(InputManager.Instance.GetKey("freezeKeybind", KeyCode.Space)))
        {
            isFrozen = !isFrozen;
            if (isFrozen)
            {
                // Kill all momentum for a clean stop BEFORE setting kinematic
                // Unity throws an error if we set velocity on a kinematic body
                AuvRb.linearVelocity = Vector3.zero;
                AuvRb.angularVelocity = Vector3.zero;
                Array.Clear(inputThrusterForces, 0, inputThrusterForces.Length);
                SimulatorHUD.Instance?.Log("<color=orange>AUV FROZEN</color> - Momentum cleared.");
            }
            else
            {
                SimulatorHUD.Instance?.Log("<color=green>AUV UNFROZEN</color> - Physics active.");
            }
            AuvRb.isKinematic = isFrozen;

        }
    }

    private void HandleMovementInput()
    {
        if (isFrozen) return;

        // Ignore movement input if user is typing in HUD
        if (SimulatorHUD.Instance != null && SimulatorHUD.Instance.IsInputFocused) return;
        
        Array.Clear(inputThrusterForces, 0, inputThrusterForces.Length);
        
        if (!Input.anyKey) return;
        
        if (Input.GetKey(InputManager.Instance.GetKey("pitchKeybind", KeyCode.I)))
        {
            inputThrusterForces[5] += rotationForceOver4;
            inputThrusterForces[2] += rotationForceOver4;
            inputThrusterForces[1] -= rotationForceOver4;
            inputThrusterForces[6] -= rotationForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("yawKeybind", KeyCode.J)))
        {
            inputThrusterForces[4] += rotationForceOver4;
            inputThrusterForces[3] -= rotationForceOver4;
            inputThrusterForces[7] -= rotationForceOver4;
            inputThrusterForces[0] += rotationForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("negPitchKeybind", KeyCode.K)))
        {
            inputThrusterForces[5] -= rotationForceOver4;
            inputThrusterForces[2] -= rotationForceOver4;
            inputThrusterForces[1] += rotationForceOver4;
            inputThrusterForces[6] += rotationForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("negYawKeybind", KeyCode.L)))
        {
            inputThrusterForces[4] -= rotationForceOver4;
            inputThrusterForces[3] += rotationForceOver4;
            inputThrusterForces[7] += rotationForceOver4;
            inputThrusterForces[0] -= rotationForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("negRollKeybind", KeyCode.U)))
        {
            inputThrusterForces[5] += rotationForceOver4;
            inputThrusterForces[2] -= rotationForceOver4;
            inputThrusterForces[6] += rotationForceOver4;
            inputThrusterForces[1] -= rotationForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("rollKeybind", KeyCode.O)))
        {
            inputThrusterForces[5] -= rotationForceOver4;
            inputThrusterForces[2] += rotationForceOver4;
            inputThrusterForces[6] -= rotationForceOver4;
            inputThrusterForces[1] += rotationForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("surgeKeybind", KeyCode.W)))
        {
            inputThrusterForces[4] -= moveForceOver4;
            inputThrusterForces[3] -= moveForceOver4;
            inputThrusterForces[7] += moveForceOver4;
            inputThrusterForces[0] += moveForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("swayKeybind", KeyCode.A)))
        {
            inputThrusterForces[4] += moveForceOver2;
            inputThrusterForces[3] -= moveForceOver2;
            inputThrusterForces[7] += moveForceOver2;
            inputThrusterForces[0] -= moveForceOver2;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("negSurgeKeybind", KeyCode.S)))
        {
            inputThrusterForces[4] += moveForceOver4;
            inputThrusterForces[3] += moveForceOver4;
            inputThrusterForces[7] -= moveForceOver4;
            inputThrusterForces[0] -= moveForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("negSwayKeybind", KeyCode.D)))
        {
            inputThrusterForces[4] -= moveForceOver2;
            inputThrusterForces[3] += moveForceOver2;
            inputThrusterForces[7] -= moveForceOver2;
            inputThrusterForces[0] += moveForceOver2;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("negHeaveKeybind", KeyCode.Q)))
        {
            inputThrusterForces[5] += sinkForceOver4;
            inputThrusterForces[2] += sinkForceOver4;
            inputThrusterForces[6] += sinkForceOver4;
            inputThrusterForces[1] += sinkForceOver4;
        }
        if (Input.GetKey(InputManager.Instance.GetKey("heaveKeybind", KeyCode.E)))
        {
            inputThrusterForces[5] -= floatForceOver4;
            inputThrusterForces[2] -= floatForceOver4;
            inputThrusterForces[6] -= floatForceOver4;
            inputThrusterForces[1] -= floatForceOver4;
        }
    }
    
    private void SetThrusterForces(ThrusterForcesMsg msg)
    {
        // Thruster order matches hardware configuration (CCL):
        // 0: back_right, 1: heave_back_right, 2: heave_front_right, 3: front_right
        // 4: front_left, 5: heave_front_left, 6: heave_back_left, 7: back_left
        rosThrusterForces[0] = msg.back_right;
        rosThrusterForces[1] = msg.heave_back_right;
        rosThrusterForces[2] = msg.heave_front_right;
        rosThrusterForces[3] = msg.front_right;
        rosThrusterForces[4] = msg.front_left;
        rosThrusterForces[5] = msg.heave_front_left;
        rosThrusterForces[6] = msg.heave_back_left;
        rosThrusterForces[7] = msg.back_left;
    }

    private void InitializeEfficiency()
    {
        for (int i = 0; i < thrusterEfficiencyScalars.Length; i++)
        {
            // Random scalar between 1.0 - variance and 1.0 + variance
            // e.g. if variance is 0.1, range is 0.9 to 1.1
            float randomOffset = UnityEngine.Random.Range(-efficiencyVariance, efficiencyVariance);
            thrusterEfficiencyScalars[i] = 1.0f + randomOffset;
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || arrowMat == null) return;
        
        // Update material color
        if (arrowMat.HasProperty("_BaseColor")) arrowMat.SetColor("_BaseColor", visualizationColor);
        if (arrowMat.HasProperty("_Color")) arrowMat.SetColor("_Color", visualizationColor);
        
        // Update MPB on all instances for X-Ray
        if (arrowInstances != null)
        {
            foreach (var arrow in arrowInstances)
            {
                if (arrow == null) continue;
                var renders = arrow.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renders)
                {
                   VisualizationUtils.SetColorProperty(r, visualizationColor);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (arrowMat != null) Destroy(arrowMat);
    }
}
