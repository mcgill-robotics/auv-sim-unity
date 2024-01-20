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
        transform.position = Diana.position;
        var lookPos = pinger.position - transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);
    }
}