using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AUVKeyboardControl : MonoBehaviour
{
    public float sinkForce = 5f;
    public float floatForce = 5f;
    public float moveForce = 5f;
    public float rotationForce = 5f;

    private Rigidbody rb;
    private bool isFrozen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        HandleFreezeInput();
    }

    void FixedUpdate()
    {
        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        if (!isFrozen)
        {
        
            // control orientation
            if (Input.GetKey(KeyCode.I)) {
                rb.AddTorque(rb.transform.TransformDirection(new Vector3(0f, 0f, -rotationForce)));
            }
            if (Input.GetKey(KeyCode.J)) {
                rb.AddTorque(rb.transform.TransformDirection(new Vector3(0f, -rotationForce, 0f)));
            }
            if (Input.GetKey(KeyCode.K)) {
                rb.AddTorque(rb.transform.TransformDirection(new Vector3(0f, 0f, rotationForce)));
            }
            if (Input.GetKey(KeyCode.L)) {
                rb.AddTorque(rb.transform.TransformDirection(new Vector3(0f, rotationForce, 0f)));
            }
            if (Input.GetKey(KeyCode.U)) {
                rb.AddTorque(rb.transform.TransformDirection(new Vector3(rotationForce, 0f, 0f)));
            }
            if (Input.GetKey(KeyCode.O)) {
                rb.AddTorque(rb.transform.TransformDirection(new Vector3(-rotationForce, 0f, 0f)));
            }
            //control position
            if (Input.GetKey(KeyCode.W)) {
                rb.AddForce(transform.right * moveForce);
            }
            if (Input.GetKey(KeyCode.A)) {
                rb.AddForce(transform.forward * moveForce);
            }
            if (Input.GetKey(KeyCode.S)) {
                rb.AddForce(-transform.right * moveForce);
            }
            if (Input.GetKey(KeyCode.D)) {
                rb.AddForce(-transform.forward * moveForce);
            }
            if (Input.GetKey(KeyCode.Q)) {
                rb.AddForce(transform.up * floatForce);
            }
            if (Input.GetKey(KeyCode.E)) {
                rb.AddForce(-transform.up * sinkForce);
            }
            
        }
    }

    void HandleFreezeInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isFrozen = !isFrozen;

            if (isFrozen)
            {
                rb.isKinematic = true;
            } else {
                rb.isKinematic = false;
            }
        }
    }
}