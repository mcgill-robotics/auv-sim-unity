using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Applies buoyancy force to the AUV. Force is applied at the center of buoyancy,
/// which creates a righting torque when offset from the center of mass.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HydrodynamicDrag))]
[RequireComponent(typeof(WaterSurface))]
public class Buoyancy : MonoBehaviour
{
    [Header("Buoyancy Configuration")]
    [Tooltip("Point where buoyancy force is applied (local coordinates). Should be above COM for stability.")]
    public Vector3 centerOfBuoyancy;

    [Tooltip("Center of mass offset (local coordinates). Applied to Rigidbody on Start.")]
    public Vector3 centerOfMass;

    [Tooltip("List of box colliders that approximate the AUV's shape for volume calculation. Top and Chassis should be sufficient for a rough estimate.")]
    [SerializeField] private List<BoxCollider> auvBoxes;

    [Tooltip("Reference to the WaterSurface component for projecting points onto the water surface")]
    [SerializeField] private WaterSurface waterSurface;

    [Tooltip("Reference to the HydrodynamicDrag component to access water density for buoyancy calculations")]
    [SerializeField] private HydrodynamicDrag hydrodynamicDrag;

    [Header("Debug")]
    [Tooltip("Log buoyancy force each frame")]
    public bool debugLogging = false;

    // cached reference to the AUV's Rigidbody for applying forces and setting center of mass
    private Rigidbody auvRb;

    // stored for visualization in OnDrawGizmos
    private Vector3 surfaceToCoB;
    // displaced volume approximation
    private float auvVolume;
    // buoyancy force does not depend on depth, it can be computed once at the start
    private float buoyancyForceMagnitude;


    /// <summary>
    /// Called when a value changes in the Inspector. Updates the Rigidbody's center of mass immediately.
    /// </summary>
    private void OnValidate()
    {
        // Get or cache the rigidbody reference
        if (auvRb == null)
            auvRb = GetComponent<Rigidbody>();

        if (auvRb != null)
        {
            auvRb.centerOfMass = centerOfMass;
        }
    }

    private void Start()
    {
        auvRb = GetComponent<Rigidbody>();
        auvRb.centerOfMass = centerOfMass;

        Bounds combinedBounds = auvBoxes[0].bounds;

        for (int i = 1; i < auvBoxes.Count; i++)
        {
            combinedBounds.Encapsulate(auvBoxes[i].bounds);
        }
        // very crude approximation of volume based on bounding box, but should be sufficient for scaling buoyancy force
        auvVolume = combinedBounds.size.x * combinedBounds.size.y * combinedBounds.size.z;

        // Archimedes' principle: Buoyant force = density of fluid * volume of displaced fluid * gravity
        buoyancyForceMagnitude = hydrodynamicDrag.waterDensity * auvVolume * Physics.gravity.magnitude;
    }


    private void FixedUpdate()
    {
        // Allow runtime tweaking of COM
#if UNITY_EDITOR
        if (auvRb.centerOfMass != centerOfMass)
        {
            auvRb.centerOfMass = centerOfMass;
        }
#endif
        ApplyBuoyancyForce(transform.TransformPoint(centerOfBuoyancy));
    }

    /// <summary>
    /// Applies simple buoyancy force (Archimedes principle) at the given world position. Point is refered to as floater to keep buoyancy force application flexible enought o accept multiple floating points in the future. For now though, it should just be applied to the center of buoyancy, since any additional points would average out to applying a single force at the center of buoyancy anyway.
    /// </summary>
    /// <param name="floaterPosition"></param>
    void ApplyBuoyancyForce(Vector3 floaterPosition)
    {
        bool isBelowWater;
        Vector3 surfaceProjection;
        if (SimulationSettings.Instance.NoWaterMode)
        {
            isBelowWater = floaterPosition.y < waterSurface.transform.position.y;
            surfaceProjection = new Vector3(floaterPosition.x, waterSurface.transform.position.y, floaterPosition.z);

        }
        else
        {
            WaterSearchParameters waterSearchParams = new WaterSearchParameters
            {
                startPositionWS = Vector3.zero,
                targetPositionWS = floaterPosition,
                error = 0.01f,
                maxIterations = 8,
                includeDeformation = false, // Ignore water deformation for buoyancy force application for easier computation
            };
            isBelowWater = waterSurface.ProjectPointOnWaterSurface(waterSearchParams, out WaterSearchResult projectedPoint);
            surfaceProjection = projectedPoint.projectedPositionWS;
        }
        if (isBelowWater)
        {
            surfaceToCoB = surfaceProjection - floaterPosition;
            float submergedDepth = surfaceToCoB.y;

            if (submergedDepth > 0)
            {
                // Floater is submerged, apply upward buoyancy force
                Vector3 buoyancyForce = Vector3.up * buoyancyForceMagnitude;

                auvRb.AddForceAtPosition(buoyancyForce, floaterPosition, ForceMode.Force);
                if (debugLogging)
                {
                    Debug.Log($"Applying buoyancy force of {buoyancyForce} N at {floaterPosition} (submerged depth: {submergedDepth:F2} m)");
                }
            }

        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.02f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfBuoyancy), 0.02f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.TransformPoint(centerOfBuoyancy) + surfaceToCoB, transform.TransformPoint(centerOfBuoyancy));


        Bounds combinedBounds = auvBoxes[0].bounds;

        for (int i = 1; i < auvBoxes.Count; i++)
        {
            combinedBounds.Encapsulate(auvBoxes[i].bounds);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
    }
}