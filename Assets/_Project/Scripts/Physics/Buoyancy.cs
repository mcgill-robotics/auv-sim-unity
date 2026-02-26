using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Applies buoyancy force to the AUV. Force is applied at the center of buoyancy,
/// which creates a righting torque when offset from the center of mass.
/// </summary>
public class Buoyancy : MonoBehaviour
{
    [Header("Buoyancy Configuration")]
    [Tooltip("Total upward buoyancy force in Newtons (should slightly exceed weight for positive buoyancy)")]
    public float buoyancyForce;

    [Tooltip("Point where buoyancy force is applied (local coordinates). Should be above COM for stability.")]
    public Vector3 centerOfBuoyancy;

    [Tooltip("Center of mass offset (local coordinates). Applied to Rigidbody on Start.")]
    public Vector3 centerOfMass;

    public List<GameObject> floaters;

    public WaterSurface waterSurface;

    private Rigidbody auvRb;

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

        float auvDistBelowSurface = -Math.Min(0, auvRb.transform.position.y);
        Vector3 forcePoint = transform.TransformPoint(centerOfBuoyancy);

        if (auvDistBelowSurface < auvLengthOver4)
        {
            // AUV is partially submerged, apply buoyancy force scaled to the submerged volume
            auvRb.AddForceAtPosition(auvDistBelowSurface * buoyancyForceVectorScaled, forcePoint, ForceMode.Force);
        }
        else
        {
            // AUV is fully submerged, apply full buoyancy force
            auvRb.AddForceAtPosition(buoyancyForceVector, forcePoint, ForceMode.Force);
        }
        foreach (GameObject floater in floaters)
        {
            ApplyBuoyancyForce(floater);
        }
    }
    // inspired by https://www.youtube.com/watch?v=vzqoLJmpUqU
    void ApplyBuoyancyForce(GameObject floater)
    {

        WaterSearchParameters waterSearchParams = new WaterSearchParameters
        {
            startPositionWS = Vector3.zero,
            targetPositionWS = floater.transform.position,
            error = 0.01f,
            maxIterations = 8,
            includeDeformation = false, // Ignore water deformation for buoyancy force application for easier computation
        };

        if (waterSurface.ProjectPointOnWaterSurface(waterSearchParams, out WaterSearchResult projectedPoint))
        {
            Debug.Log($"Projected Position = {projectedPoint.projectedPositionWS}, Normal = {projectedPoint.normalWS}, Error = {projectedPoint.error}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.02f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfBuoyancy), 0.02f);
    }
}