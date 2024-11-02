using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    Rigidbody auv;
    public float buoyancyForce;

    private float auvLengthOver4;
    private Vector3 buoyancyForceVector;
    private Vector3 buoyancyForceVectorOverAuvLength;

    // Start is called before the first frame update
    void Start()
    {
        auv = GetComponent<Rigidbody>();
        auvLengthOver4 = auv.transform.localScale.x / 4;
        buoyancyForceVector = Vector3.up * buoyancyForce;
        buoyancyForceVectorOverAuvLength = buoyancyForceVector / auvLengthOver4;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float level = Math.Min(0, auv.transform.position.y);
        if (level > -auv.transform.localScale.x / 4)
        {
            auv.AddForceAtPosition(Math.Abs(level) * buoyancyForceVectorOverAuvLength, transform.position, ForceMode.Force);
        }
        else
        {
            auv.AddForceAtPosition(buoyancyForceVector, transform.position, ForceMode.Force);
        }
    }
}