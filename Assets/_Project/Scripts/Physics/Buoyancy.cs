using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private Rigidbody auvRb;
    private float auvLengthOver4;
    private Vector3 buoyancyForceVector;
    private Vector3 buoyancyForceVectorScaled;

    
    private void Start()
    {
        auvRb = GetComponent<Rigidbody>();
        auvRb.centerOfMass = centerOfMass;
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
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfBuoyancy), 0.05f);
    }
}