using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace Actuators
{
    /// <summary>
    /// Controls the torpedo launcher mechanism: rotates a base and launches torpedos.
    /// Supports both ROS commands and manual keyboard override.
    /// </summary>
    public class TorpedoLauncher : MonoBehaviour
    {
        [Header("Launcher Configuration")]
        [Tooltip("The base transform that rotates")]
        public Transform rotatingBase;
        
        [Tooltip("The two torpedo gameobjects")]
        public GameObject[] torpedos;
        
        [Tooltip("Maximum rotation angle from center (80 degrees)")]
        public float maxRotationAngle = 80f;
        
        [Tooltip("Speed of rotation (degrees per second)")]
        public float rotationSpeed = 30f;
        
        [Tooltip("Initial forward velocity when launched")]
        public float launchForce = 10f;

        [Header("Torpedo Physics Configuration")]
        [Tooltip("Mass of the torpedo when launched")]
        public float torpedoMass = 1.0f;
        
        [Tooltip("Linear drag of the torpedo")]
        public float torpedoDrag = 0.5f;
        
        [Tooltip("Angular drag of the torpedo")]
        public float torpedoAngularDrag = 0.5f;
        
        [Tooltip("Layers to exclude from collision (Unity 6+)")]
        public LayerMask excludeLayers;

        [Header("Outline Configuration")]
        [Tooltip("If true, an outline will be applied to the torpedo when launched")]
        public bool showOutlineOnLaunch = true;
        
        [Tooltip("Color of the outline")]
        public Color outlineColor = Color.yellow;
        
        [Tooltip("Width of the outline")]
        public float outlineWidth = 0.02f;
        
        [Tooltip("The outline shader to use (Custom/TorpedoOutline)")]
        public Shader outlineShader;

        [Header("Collider Configuration")]
        [Tooltip("If true, the script will enable/disable existing colliders on launch/reset")]
        public bool manageColliders = true;

        [Header("Manual Control (Read Only)")]
        [SerializeField] private int nextTorpedoIndex = 0;
        [SerializeField] private float targetRotation = 0f;

        private ROSConnection roscon;

        // Store initial states for resetting
        private struct TorpedoState
        {
            public Transform parent;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Material[][] originalMaterials; // Store materials for each renderer
        }
        private TorpedoState[] initialStates;
        private Material outlineMat;

        private void Start()
        {
            roscon = ROSConnection.GetOrCreateInstance();
            
            // Subscribe to ROS topics
            roscon.Subscribe<BoolMsg>(ROSSettings.Instance.TorpedoLaunchTopic, OnRosLaunch);
            roscon.Subscribe<BoolMsg>(ROSSettings.Instance.TorpedoResetTopic, OnRosReset);
            roscon.Subscribe<Float32Msg>(ROSSettings.Instance.TorpedoRotationTopic, OnRosRotate);

            // Store initial states
            initialStates = new TorpedoState[torpedos.Length];
            for (int i = 0; i < torpedos.Length; i++)
            {
                if (torpedos[i] == null) continue;
                
                initialStates[i] = new TorpedoState
                {
                    parent = torpedos[i].transform.parent,
                    localPosition = torpedos[i].transform.localPosition,
                    localRotation = torpedos[i].transform.localRotation,
                    originalMaterials = StoreOriginalMaterials(torpedos[i])
                };

                // Ensure they don't have a Rigidbody initially
                Rigidbody existingRb = torpedos[i].GetComponent<Rigidbody>();
                if (existingRb != null)
                {
                    Destroy(existingRb);
                }

                // Disable collider initially if managing them
                if (manageColliders)
                {
                    Collider col = torpedos[i].GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        private void Update()
        {
            HandleManualInput();
            UpdateRotation();
        }

        private void HandleManualInput()
        {
            // Ignore if typing in HUD
            if (SimulatorHUD.Instance != null && SimulatorHUD.Instance.IsInputFocused) return;

            // Launch (T key)
            if (Input.GetKeyDown(InputManager.Instance.GetKey("torpedoLaunchKeybind", KeyCode.T)))
            {
                LaunchTorpedo();
            }

            // Reset (Y key)
            if (Input.GetKeyDown(InputManager.Instance.GetKey("torpedoResetKeybind", KeyCode.Y)))
            {
                ResetLauncher();
            }

            // Rotate (Left/Right or [ / ])
            float rotInput = 0f;
            if (Input.GetKey(InputManager.Instance.GetKey("torpedoRotateLeftKeybind", KeyCode.LeftArrow))) rotInput -= 1f;
            if (Input.GetKey(InputManager.Instance.GetKey("torpedoRotateRightKeybind", KeyCode.RightArrow))) rotInput += 1f;
            
            if (rotInput != 0)
            {
                targetRotation += rotInput * rotationSpeed * Time.deltaTime;
                targetRotation = Mathf.Clamp(targetRotation, -maxRotationAngle, maxRotationAngle);
            }
        }

        private void UpdateRotation()
        {
            if (rotatingBase == null) return;
            
            // Smoothly rotate towards target
            Quaternion targetRot = Quaternion.Euler(0, targetRotation, 0);
            rotatingBase.localRotation = Quaternion.RotateTowards(rotatingBase.localRotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        private void OnRosLaunch(BoolMsg msg)
        {
            if (msg.data) LaunchTorpedo();
        }

        private void OnRosReset(BoolMsg msg)
        {
            if (msg.data) ResetLauncher();
        }

        private void OnRosRotate(Float32Msg msg)
        {
            targetRotation = Mathf.Clamp(msg.data, -maxRotationAngle, maxRotationAngle);
        }

        private Material[][] StoreOriginalMaterials(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Material[][] mats = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                mats[i] = renderers[i].sharedMaterials;
            }
            return mats;
        }

        private void ApplyOutline(GameObject obj)
        {
            if (!showOutlineOnLaunch) return;
            
            if (outlineMat == null)
            {
                Shader shader = outlineShader != null ? outlineShader : Shader.Find("Custom/TorpedoOutline");
                if (shader == null) return;
                outlineMat = new Material(shader);
            }
            
            outlineMat.SetColor("_OutlineColor", outlineColor);
            outlineMat.SetFloat("_OutlineWidth", outlineWidth);

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var ren in renderers)
            {
                Material[] currentMats = ren.sharedMaterials;
                Material[] newMats = new Material[currentMats.Length + 1];
                for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
                newMats[currentMats.Length] = outlineMat;
                ren.materials = newMats; // Note: accessing .materials creates clones, but since we are launching it's okay. Actually better move to .sharedMaterials if possible or use PropertyBlocks? But hull outline NEEDS a second pass.
            }
        }

        private void RemoveOutline(int torpedoIndex)
        {
            GameObject torpedo = torpedos[torpedoIndex];
            if (torpedo == null) return;

            Renderer[] renderers = torpedo.GetComponentsInChildren<Renderer>();
            Material[][] originalMats = initialStates[torpedoIndex].originalMaterials;

            if (renderers.Length != originalMats.Length) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].materials = originalMats[i];
            }
        }

        [ContextMenu("Launch Torpedo")]
        public void LaunchTorpedo()
        {
            if (nextTorpedoIndex >= torpedos.Length)
            {
                Debug.LogWarning("[TorpedoLauncher] No more torpedos to launch!");
                return;
            }

            GameObject torpedo = torpedos[nextTorpedoIndex];
            if (torpedo == null) return;

            // Add Rigidbody on the fly
            Rigidbody rb = torpedo.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = torpedo.AddComponent<Rigidbody>();
            }

            // Configure Rigidbody
            rb.mass = torpedoMass;
            rb.linearDamping = torpedoDrag;
            rb.angularDamping = torpedoAngularDrag;
            rb.excludeLayers = excludeLayers; // Exclude layers from collision (Unity 6 feature)
            rb.useGravity = false; // Usually true for underwater if buoyancy isn't handled separately, but let's assume neutrally buoyant or handled by other scripts
            rb.isKinematic = false;

            // Handle Collider
            if (manageColliders)
            {
                Collider col = torpedo.GetComponent<Collider>();
                if (col != null)
                {
                    col.excludeLayers = excludeLayers;
                    col.enabled = true;
                }
            }

            torpedo.transform.parent = null; // Detach
            rb.linearVelocity = torpedo.transform.forward * launchForce;
            
            ApplyOutline(torpedo);
            
            Debug.Log($"[TorpedoLauncher] Launched torpedo {nextTorpedoIndex + 1}");
            nextTorpedoIndex++;
        }

        [ContextMenu("Reset Launcher")]
        public void ResetLauncher()
        {
            for (int i = 0; i < torpedos.Length; i++)
            {
                if (torpedos[i] == null) continue;

                // Remove Rigidbody if it exists
                Rigidbody rb = torpedos[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Destroy(rb);
                }

                // Disable collider
                if (manageColliders)
                {
                    Collider col = torpedos[i].GetComponent<Collider>();
                    if (col != null)
                    {
                        col.enabled = false;
                    }
                }

                torpedos[i].transform.parent = initialStates[i].parent;
                torpedos[i].transform.localPosition = initialStates[i].localPosition;
                torpedos[i].transform.localRotation = initialStates[i].localRotation;
                
                RemoveOutline(i);
            }

            nextTorpedoIndex = 0;
            targetRotation = 0f;
            if (rotatingBase != null) rotatingBase.localRotation = Quaternion.identity;
            
            Debug.Log("[TorpedoLauncher] Launcher reset.");
        }

        private void OnDestroy()
        {
            if (outlineMat != null) Destroy(outlineMat);
        }
    }
}
