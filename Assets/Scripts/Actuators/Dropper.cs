using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class Dropper : MonoBehaviour
{
    public GameObject sphere;

    private ROSConnection roscon;
    private Rigidbody sphereRb;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Transform initialParent;

    void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<BoolMsg>(ROSSettings.Instance.DropperTopic, DropDropper);
        sphereRb = sphere.GetComponent<Rigidbody>();
        
        // Store initial state
        initialParent = sphere.transform.parent;
        initialLocalPos = sphere.transform.localPosition;
        initialLocalRot = sphere.transform.localRotation;
    }

    void Update()
    {
        if (Input.GetKeyDown(InputManager.Instance.GetKey("dropperKeybind", KeyCode.G)))
        {
            DropDropper(new BoolMsg(true));
        }
    }

    void DropDropper(BoolMsg msg)
    {
        if (msg.data)
        {
            sphereRb.isKinematic = false;
            sphere.transform.parent = null;
        }
    }

    public void ResetDropper()
    {
        sphereRb.isKinematic = true;
        sphere.transform.parent = initialParent;
        sphere.transform.localPosition = initialLocalPos;
        sphere.transform.localRotation = initialLocalRot;
        sphereRb.linearVelocity = Vector3.zero;
        sphereRb.angularVelocity = Vector3.zero;
    }
}
