using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruePingerBearing : MonoBehaviour {
    public Transform Diana;
    public Transform hydrophone1;
    public Transform hydrophone2;
    public Transform hydrophone3;
    public Transform pinger;
    int damping = 5;

    void Update() {
        transform.position = Vector3.MoveTowards(transform.position, Diana.position, 5.0f);
        var lookPos = pinger.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping); 
    }
}