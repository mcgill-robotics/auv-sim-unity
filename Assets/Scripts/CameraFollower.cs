using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public GameObject targetObject;
    public float rigidity = 10f;

    private Vector3 offsetPosition;
    private Quaternion offsetRotation;

    // Awake is called before Start
    void Awake()
    {
        offsetPosition = offsetPosition = targetObject.transform.InverseTransformPoint(transform.position);
        offsetRotation = Quaternion.Inverse(targetObject.transform.rotation) * transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 relativeOffset = targetObject.transform.TransformPoint(offsetPosition);
        transform.position = Vector3.Lerp(transform.position, relativeOffset, Time.deltaTime * rigidity);

        Quaternion relativeRotation = targetObject.transform.rotation * offsetRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, relativeRotation, Time.deltaTime * rigidity);
    }
}
