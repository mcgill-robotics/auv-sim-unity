using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// Controls the dropper mechanism: releases a sphere on ROS command or G key press.
/// </summary>
public class Dropper : MonoBehaviour
{
    [Header("Dropper Configuration")]
    [Tooltip("The sphere GameObject to drop when triggered")]
    public GameObject sphere;

    [SerializeField]
    [Tooltip("The transform to the collider area")]
    public Collider grabberArea;

    [Tooltip("Sets whether a grab teleports the object to a fixed position under grabber")]
    public bool simpleGrab;

    private ROSConnection roscon;
    private Rigidbody sphereRb;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Transform initialParent;
    private List<Rigidbody> candidateObjects = new List<Rigidbody>();
    private List<Rigidbody> candidateObjectsReadyForGrab = new List<Rigidbody>();
    private List<Rigidbody> grippedObjects = new List<Rigidbody>(); // List to track currently gripped objects (technically should only be one for the dropper, but allows for future expansion)

    void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<UInt8Msg>(ROSSettings.Instance.GrabberTopic, OnGrabberCommand);
#pragma warning disable CS0618
        roscon.Subscribe<BoolMsg>(ROSSettings.Instance.DropperTopic, DropDropper);
        roscon.Subscribe<BoolMsg>(ROSSettings.Instance.GripperTopic, GripObject);
#pragma warning restore CS0618
        sphereRb = sphere.GetComponent<Rigidbody>();
        
        // Store initial state
        initialParent = sphere.transform.parent;
        initialLocalPos = sphere.transform.localPosition;
        initialLocalRot = sphere.transform.localRotation;
        sphereRb.isKinematic = true;
        grippedObjects.Add(sphereRb);

        // Get the collider defining the grab area for object detection
        if (grabberArea == null)
        {
            Debug.LogError("Dropper GameObject must have a Collider component child to define the grab area");
            return;
        }
    }

    private void OnGrabberCommand(UInt8Msg msg)
    {
        // 0 = close/grab, >0 = open/release/drop
        if (msg.data > 0)
        {
            DropDropper(new BoolMsg(true));
        }
        else
        {
            GripObject(new BoolMsg(true));
        }
    }

    void Update()
    {
        // Check if there is any object within the grab area
        CheckGrippableObjectWithinArea();

        if (!InputGate.GameKeysEnabled) return;
        if (SimulatorHUD.Instance != null && SimulatorHUD.Instance.IsInputFocused) return;

        // Check for dropper trigger input (G key by default)
        if (Input.GetKeyDown(InputManager.Instance.GetKey("dropperKeybind", KeyCode.G)))
        {   
            DropDropper(new BoolMsg(true));
        }

        // Check for gripping input (H key by default)
        if (Input.GetKeyDown(InputManager.Instance.GetKey("gripperKeybind", KeyCode.H)))
        {
            GripObject(new BoolMsg(true));
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
        grippedObjects.Clear();
        grippedObjects.Add(sphereRb);
    }

    void DropDropper(BoolMsg msg)
    {
        if (msg.data)
        {
            if (grippedObjects.Count == 0)
            {
                Debug.LogWarning("Dropper trigger received but no object is currently gripped");
                return;
            }

            for (int i = 0; i < grippedObjects.Count; i++)
            {
                if (grippedObjects[i] != null)
                {
                    grippedObjects[i].isKinematic = false;
                    grippedObjects[i].transform.parent = null;
                }
            }
            grippedObjects.Clear();
        }
    }

    void GripObject(BoolMsg msg)
    {
        if (msg.data)
        {
            if (candidateObjectsReadyForGrab.Count == 0)
            {
                Debug.LogWarning("Gripper trigger received but no object is within the grab area");
                return;
            }

            // Start a coroutine to grip the object after a short delay 
            // to simulate the time it takes for the gripper to close
            // and ensure the object is still within grab area
            StartCoroutine(GripObjectCoroutine());
        }
    }

    private void CheckGrippableObjectWithinArea()
    {
        if (grabberArea == null) return;

        candidateObjectsReadyForGrab.Clear();
        Bounds areaBounds = grabberArea.bounds;

        for (int i = 0; i < candidateObjects.Count; i++)
        {
            if (candidateObjects[i] == null) continue;
            Collider col = candidateObjects[i].GetComponent<Collider>();
            if (col == null) continue;

            if (areaBounds.Contains(col.bounds.center))
            {
                candidateObjectsReadyForGrab.Add(candidateObjects[i]);
            }
        }
    }

    // Coroutine to grip an object after a short delay, an object needs to be 
    // both at the start of command and after the delay to be gripped. This is not 
    // perfect simulation of a gripper closing mechanism, but it adds some realism by simulating the time 
    // it takes for the gripper to close and ensuring the object is still within the grab area after that time before gripping it
    private IEnumerator GripObjectCoroutine()
    {
        // Make a snapshot of all candidate objects currently within the grab area 
        // at the time the grip command is received,
        List<Rigidbody> objectsToGrip = new List<Rigidbody>(candidateObjectsReadyForGrab);
        Debug.Log("Checking object for gripping: " + objectsToGrip[0].transform.position);
        // Start a timer for 2 seconds to simulate the time it takes for the gripper to close,
        yield return new WaitForSeconds(2.0f);

        // After the delay, check if the objects in the snapshot are still within the grab area before gripping
        for (int i = 0; i < objectsToGrip.Count; i++)
        {
            Debug.Log("Checking object for grab: "  + transform.position);
            Debug.Log("Checking object for gripping: " + objectsToGrip[i].transform.position);

            if (candidateObjectsReadyForGrab.Contains(objectsToGrip[i]))
            {
                // grippy
                objectsToGrip[i].isKinematic = true;
                objectsToGrip[i].transform.parent = transform;

                if (simpleGrab)
                {
                    // Teleport the object to a fixed position under the grabber for a simple grab simulation
                    objectsToGrip[i].transform.localPosition = new Vector3(0.0245f, -0.07f, 0f);
                }
                grippedObjects.Add(objectsToGrip[i]);
            }
        }
    }

    // -- COLLISION TRIGGERS FOR OBJECT DETECTION WITHIN GRAB AREA --

    // Function to detect if there is an object within the dropper's grab area
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Object entered dropper grab area: " + other.gameObject.name);
        // Check if the object has a Rigidbody and is not already kinematic
        Rigidbody candidateObject = other.GetComponent<Rigidbody>();

        // Check the candidate object is not null and is not kinematic (i.e., it can be gripped)
        if (candidateObject == null || candidateObject.isKinematic)
        {
            return; // No valid object to grip
        }

        // Check if the object is of tag to be gripped, so we dont grip everything in the area
        if (!other.CompareTag("Grippable"))
        {
            return;
        }

        candidateObjects.Add(candidateObject);
    }

    // private void OnTriggerStay(Collider other)
    // {
    //     Debug.Log("Object staying in dropper grab area: " + other.gameObject.name);
    // }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody candidateObject = other.GetComponent<Rigidbody>();
        if (candidateObject != null && other.CompareTag("Grippable"))
        {
            candidateObjects.Remove(candidateObject);
        }
    }

}
