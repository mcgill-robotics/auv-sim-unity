using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    Rigidbody auvRb;
    public float buoyancyForce;

    private float auvLengthOver4;
    private Vector3 buoyancyForceVector;
    private Vector3 buoyancyForceVectorScaled;

    // Start is called before the first frame update
    void Start()
    {
        auvRb = GetComponent<Rigidbody>();
        auvLengthOver4 = auvRb.transform.localScale.x / 4;
        buoyancyForceVector = Vector3.up * buoyancyForce;
        buoyancyForceVectorScaled = buoyancyForceVector / auvLengthOver4;
    }

    // Update is called once per frame
    void FixedUpdate()
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