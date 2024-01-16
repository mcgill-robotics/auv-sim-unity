using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingerBearing : MonoBehaviour
{
    public Transform pinger1;
    public Transform Diana;

    private Vector3 targetDirection;
    private Vector3 newDirection;
    void Start()
    {
        targetDirection = pinger1.position - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        newDirection = Vector3.RotateTowards(transform.position, targetDirection, 5.0f, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDirection);
    }
    void Update()
    {
        targetDirection = pinger1.position - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        newDirection = Vector3.RotateTowards(transform.position, targetDirection, 5.0f, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDirection);
    }
}