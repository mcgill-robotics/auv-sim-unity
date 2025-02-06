using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    public float buoyancyForce;

    private Rigidbody auvRb;

    private float auvLengthOver4;
    private Vector3 buoyancyForceVector;
    private Vector3 buoyancyForceVectorScaled;

    
    private void Start()
    {
        auvRb = GetComponent<Rigidbody>();
        auvLengthOver4 = auvRb.transform.localScale.x / 4;
        buoyancyForceVector = Vector3.up * buoyancyForce;
        buoyancyForceVectorScaled = buoyancyForceVector / auvLengthOver4;
    }

    private void FixedUpdate()
    {
        float auvDistBelowSurface = -Math.Min(0, auvRb.transform.position.y);
        if (auvDistBelowSurface < auvLengthOver4)
        {
            // AUV is partially submerged, apply buoyancy force scaled to the submerged volume
            auvRb.AddForceAtPosition(auvDistBelowSurface * buoyancyForceVectorScaled, transform.position, ForceMode.Force);
        }
        else
        {
            // AUV is fully submerged, apply full buoyancy force
            auvRb.AddForceAtPosition(buoyancyForceVector, transform.position, ForceMode.Force);
        }
    }
}