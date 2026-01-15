using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torpedo : MonoBehaviour
{
    public float moveSpeed;
    public float spinSpeed;
    public Rigidbody torpedoRigidbody;
    // Start is called before the first frame update
    void Start()
    {
        torpedoRigidbody.AddForce(transform.forward * moveSpeed, ForceMode.VelocityChange);
        torpedoRigidbody.AddTorque(transform.forward * spinSpeed, ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
