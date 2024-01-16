using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingerBearing : MonoBehaviour
{
    public Transform pinger1;
    public Transform Diana;

    int damping = 5;

    void Start()
    {
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger1.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger1.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }
}