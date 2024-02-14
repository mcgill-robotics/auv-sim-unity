using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyFishGroup2 : MonoBehaviour {
    /* This fish only moves back and forward on Z axis */
    float target1_z = 36.74f;
    float target2_z = 21.05f;
    float speed = 3.0f;
    bool isMovingToTarget1 = true; // if false, move to target2
    Vector3 target1;
    Vector3 target2;

    void Start() {
        target1 = new Vector3(transform.position.x, transform.position.y, target1_z);
        target2 = new Vector3(transform.position.x, transform.position.y, target2_z);
    }

    void Update() {
        // Move our position a step closer to the target.
        var step =  speed * Time.deltaTime; // calculate distance to move
        if (isMovingToTarget1) {
            transform.position = Vector3.MoveTowards(transform.position, target1, step);
            if (Vector3.Distance(transform.position, target1) < 0.1f) {
                isMovingToTarget1 = false;
            }
        } else {
            transform.position = Vector3.MoveTowards(transform.position, target2, step);
            if (Vector3.Distance(transform.position, target2) < 0.1f) {
                isMovingToTarget1 = true;
            }
        }
    }
}