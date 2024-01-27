using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruePingerBearing : MonoBehaviour {
    public Transform Diana;
    public Transform pinger;

    void Update() {
        transform.position = Diana.position;
        var lookPos = pinger.position - transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);
    }
}