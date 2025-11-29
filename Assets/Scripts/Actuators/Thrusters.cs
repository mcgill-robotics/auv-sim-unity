using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Auv;

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
    public float forceVisualScale = 0.02f;
    
    private GameObject[] arrowInstances;

    private ROSConnection roscon;
    private bool isFrozen = false;
    private Rigidbody auvRb;
    private double[] rosThrusterForces = new double[8];
    private double[] inputThrusterForces = new double[8];
    private float massScalarRealToSim;
    
    // Pre-calculated force multipliers
    private float moveForceOver4, moveForceOver2, sinkForceOver4, floatForceOver4, rotationForceOver4;
    
    // Cached quality level to avoid repeated calls
    private int cachedQualityLevel;

    private void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        auvRb = GetComponent<Rigidbody>();
        roscon.Subscribe<ThrusterForcesMsg>(ROSSettings.Instance.ThrusterForcesTopic, SetThrusterForces);
        
        massScalarRealToSim = 1f / AUVRealForceMultiplier;
        moveForceOver4 = moveForce / 4;
        moveForceOver2 = moveForce / 2;
        sinkForceOver4 = sinkForce / 4;
        floatForceOver4 = floatForce / 4;
        rotationForceOver4 = rotationForce / 4;

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
        GameObject prefabToUse = CreateDefaultArrowPrefab();

        for (int i = 0; i < thrusters.Length; i++)
        {
            arrowInstances[i] = Instantiate(prefabToUse, thrusters[i].position, Quaternion.identity);
            arrowInstances[i].transform.parent = thrusters[i]; // Parent to thruster so it moves with AUV
            arrowInstances[i].SetActive(false);
        }

        // If we created a runtime prefab, destroy the template
        Destroy(prefabToUse);
    }

    private GameObject CreateDefaultArrowPrefab()
    {
        GameObject arrowRoot = new GameObject("DefaultArrow");
        
        // Shaft (Cylinder)
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.transform.parent = arrowRoot.transform;
        shaft.transform.localPosition = new Vector3(0, 0.5f, 0); // Pivot at base
        shaft.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f); // Height is 2*scaleY = 1.0
        Destroy(shaft.GetComponent<Collider>()); // No physics

        // Head (Cube as simple pointer)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.parent = arrowRoot.transform;
        head.transform.localPosition = new Vector3(0, 1.0f, 0);
        head.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        Destroy(head.GetComponent<Collider>());

        // Material color
        Renderer shaftRen = shaft.GetComponent<Renderer>();
        Renderer headRen = head.GetComponent<Renderer>();
        shaftRen.material.color = Color.red;
        headRen.material.color = Color.red;

        return arrowRoot;
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
            
            float thrusterForceMagnitude = (float)(rosThrusterForces[i] + inputThrusterForces[i]);
            Vector3 worldForceDirection = thrusters[i].TransformDirection(Vector3.up);
            Vector3 thrusterForceVector = worldForceDirection * (thrusterForceMagnitude * massScalarRealToSim);
            
            auvRb.AddForceAtPosition(thrusterForceVector, thrusters[i].position, ForceMode.Force);
            
            bool hasForce = Math.Abs(thrusterForceMagnitude) > 0.01f;

            // Particles
            if (hasForce && cachedQualityLevel < 2)
            {
                if (!thrusterParticles[i].isPlaying) thrusterParticles[i].Play();
            }
            else
            {
                thrusterParticles[i].Stop();
            }

            // 3D Arrows
            if (enableForceVisualization && arrowInstances != null && i < arrowInstances.Length)
            {
                if (hasForce)
                {
                    arrowInstances[i].SetActive(true);
                    
                    // Position: At thruster
                    arrowInstances[i].transform.position = thrusters[i].position;
                    
                    // Rotation: Point in force direction
                    if (thrusterForceVector.sqrMagnitude > 0.001f)
                    {
                        arrowInstances[i].transform.rotation = Quaternion.LookRotation(thrusterForceVector) * Quaternion.Euler(90, 0, 0); 
                        // Note: Default cylinder is Y-up. LookRotation is Z-forward. 
                        // We need to rotate 90 deg on X to align Y-up cylinder with Z-forward look.
                    }

                    // Scale: Length based on magnitude
                    float length = Math.Abs(thrusterForceMagnitude) * forceVisualScale;
                    arrowInstances[i].transform.localScale = new Vector3(1, length, 1);
                }
                else
                {
                    arrowInstances[i].SetActive(false);
                }
            }
        }
    }

    private void HandleFreezeInput()
    {
        if (Input.GetKeyDown(InputManager.Instance.GetKey("freezeKeybind", KeyCode.Space)))
        {
            isFrozen = !isFrozen;
            auvRb.isKinematic = isFrozen;
        }
    }

    private void HandleMovementInput()
    {
        if (isFrozen) return;
        
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
        rosThrusterForces[0] = msg.front_left;
        rosThrusterForces[1] = msg.front_right;
        rosThrusterForces[2] = msg.back_left;
        rosThrusterForces[3] = msg.back_right;
        rosThrusterForces[4] = msg.heave_front_left;
        rosThrusterForces[5] = msg.heave_front_right;
        rosThrusterForces[6] = msg.heave_back_left;
        rosThrusterForces[7] = msg.heave_back_right;
    }
}
