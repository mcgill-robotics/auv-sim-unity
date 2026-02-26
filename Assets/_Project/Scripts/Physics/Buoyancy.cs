using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Applies buoyancy force to the AUV. Force is applied at the center of buoyancy,
/// which creates a righting torque when offset from the center of mass.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    [Header("Buoyancy Configuration")]
    [Tooltip("Total upward buoyancy force in Newtons (should slightly exceed weight for positive buoyancy)")]
    public float buoyancyForce;

    [Tooltip("Point where buoyancy force is applied (local coordinates). Should be above COM for stability.")]
    public Vector3 centerOfBuoyancy;

    [Tooltip("Center of mass offset (local coordinates). Applied to Rigidbody on Start.")]
    public Vector3 centerOfMass;

    [SerializeField] private WaterSurface waterSurface;
    [SerializeField] private HydrodynamicDrag hydrodynamicDrag;

    public List<Vector3> floaterPositions;

    private Rigidbody auvRb;

    private Vector3 surfaceToCoB;
    private float auvVolume;

    /// <summary>
    /// Threshold depth (in meters) for partial submersion calculation.
    /// When AUV is within this distance of Y=0, buoyancy is scaled linearly.
    /// Uses AUV length / 4 as a rough approximation of the vehicle's "waterline height".
    /// </summary>
    private float auvLengthOver4;

    private Vector3 buoyancyForceVector;
    private Vector3 buoyancyForceVectorScaled;

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

        // Use 1/4 of AUV length as the "waterline zone" depth threshold
        auvLengthOver4 = auvRb.transform.localScale.x / 4;

        buoyancyForceVector = Vector3.up * buoyancyForce;
        buoyancyForceVectorScaled = buoyancyForceVector / auvLengthOver4;
        // floaterPositions = new List<Vector3>
        // {
        //     centerOfBuoyancy + transform.position
        // };
        Collider[] colliders = auvRb.GetComponentsInChildren<Collider>();

        Bounds combinedBounds = colliders[0].bounds;

        for (int i = 1; i < colliders.Length; i++)
        {
            combinedBounds.Encapsulate(colliders[i].bounds);
        }
        Debug.Log($"Combined AUV bounds: center={combinedBounds.center}, size={combinedBounds.size} Number of colliders: {colliders.Length}");
        // very crude approximation of volume based on bounding box, but should be sufficient for scaling buoyancy force
        auvVolume = combinedBounds.size.x * combinedBounds.size.y * combinedBounds.size.z;
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

        // float auvDistBelowSurface = -Math.Min(0, auvRb.transform.position.y);
        // Vector3 forcePoint = transform.TransformPoint(centerOfBuoyancy);

        // if (auvDistBelowSurface < auvLengthOver4)
        // {
        //     // AUV is partially submerged, apply buoyancy force scaled to the submerged volume
        //     auvRb.AddForceAtPosition(auvDistBelowSurface * buoyancyForceVectorScaled, forcePoint, ForceMode.Force);
        // }
        // else
        // {
        //     // AUV is fully submerged, apply full buoyancy force
        //     auvRb.AddForceAtPosition(buoyancyForceVector, forcePoint, ForceMode.Force);
        // }
        // foreach (Vector3 floater in floaterPositions)
        // {
        ApplyBuoyancyForce(transform.TransformPoint(centerOfBuoyancy));
        // }
    }

    void ApplyBuoyancyForce(Vector3 floaterPosition)
    {

        WaterSearchParameters waterSearchParams = new WaterSearchParameters
        {
            startPositionWS = Vector3.zero,
            targetPositionWS = floaterPosition,
            error = 0.01f,
            maxIterations = 8,
            includeDeformation = false, // Ignore water deformation for buoyancy force application for easier computation
        };

        if (waterSurface.ProjectPointOnWaterSurface(waterSearchParams, out WaterSearchResult projectedPoint))
        {
            if (projectedPoint.projectedPositionWS.y > floaterPosition.y)
            {
                // stored for debugging/visualization in OnDrawGizmos
                surfaceToCoB = (Vector3)projectedPoint.projectedPositionWS - floaterPosition;
                // Floater is submerged, apply upward buoyancy force
                float submergedDepth = surfaceToCoB.y;
                float waterDensity = hydrodynamicDrag.waterDensity;

                // Archimedes' principle: Buoyant force = density of fluid * volume of displaced fluid * gravity
                float buoyancyForceMagnitude = waterDensity * auvVolume * Physics.gravity.magnitude;
                Vector3 buoyancyForce = Vector3.up * buoyancyForceMagnitude;

                auvRb.AddForceAtPosition(buoyancyForce, floaterPosition, ForceMode.Force);

                Debug.Log($"Applying buoyancy force of {buoyancyForce} N at {floaterPosition} (submerged depth: {submergedDepth:F2} m)");
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
    }
}