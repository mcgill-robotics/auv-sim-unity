using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingerBearing : MonoBehaviour
{
    public Transform pinger1;
    public Transform Diana;

    private Vector3 heading;
    void Start()
    {
        heading = pinger1.position - Diana.position;
        transform.position = Vector3.MoveTowards(Diana.position, heading, 5.0f);
    }
    void Update()
    {
        heading = pinger1.position - Diana.position;
        transform.position = Vector3.MoveTowards(Diana.position, heading, 0.2f);
    }
}