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
        Quaternion default_rotation = new Quaternion(0.7071068f, 0f, 0f, 0.7071068f);
        transform.rotation = Quaternion.LookRotation(lookPos) * default_rotation;
    }
}