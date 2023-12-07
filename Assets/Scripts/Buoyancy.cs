using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
	Rigidbody auv;
	public float buoyancyForce;

    // Start is called before the first frame update
    void Start()
    {
        auv = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float level = Math.Min(0, auv.transform.position.y);
        if (level > -auv.transform.localScale.x / 4)
        {
            auv.AddForceAtPosition(Vector3.up * buoyancyForce * Math.Abs(level)/(auv.transform.localScale.x / 4), transform.position, ForceMode.Force);
        } else {
            auv.AddForceAtPosition(Vector3.up * buoyancyForce, transform.position, ForceMode.Force);
        }
    }
}
