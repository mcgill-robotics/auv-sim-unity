using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuteFish : MonoBehaviour {
    /* This fish only moves back and forward on X axis */
    float target1_x = 19.17f;
    float target2_x = 40.66f;
    float speed = 1.0f;
    bool isMovingToTarget1 = true; // if false, move to target2
    Vector3 target1;
    Vector3 target2;

    void Start() {
        target1 = new Vector3(target1_x, transform.position.y, transform.position.z);
        target2 = new Vector3(target2_x, transform.position.y, transform.position.z);
    }

    void Update() {
        // Move our position a step closer to the target.
        var step =  speed * Time.deltaTime; // calculate distance to move
        if (isMovingToTarget1) {
            Debug.Log(Vector3.Distance(transform.position, target1));
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