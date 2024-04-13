using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;
using RosMessageTypes.Geometry;

public class GrabberControl : MonoBehaviour
{
    public LayerMask grabbableLayers;
    ROSConnection roscon;
    public string grabberTopicName = "/actuators/grab";
    public Color notGrabbingColor;
    public Color grabbingColor;

    private bool grabberState = true;

    private List<GameObject> objectsInGrabber = new List<GameObject>();

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<BoolMsg>("/actuators/grab", grabberStateCb);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.G)) {
            grabberState = !grabberState;
            // publish grabber state
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((grabbableLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            objectsInGrabber.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        objectsInGrabber.Remove(other.gameObject);
    }

    void getObjectsInGrabber() {
        Collider[] colliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation, grabbableLayers);
    }

    void hideGrabber() {
        Renderer renderer = this.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = notGrabbingColor;
        }
    }
    
    void showGrabber() {
        Renderer renderer = this.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = grabbingColor;
        }
    }
    
    void grabObject() {
        foreach (GameObject obj in objectsInGrabber)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.isKinematic = true;
                rb.detectCollisions = false;
                obj.transform.parent = transform;
            }
        }
    }
    
    void dropObject() {
        foreach (GameObject obj in objectsInGrabber)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                obj.transform.parent = null;
            }
        }
    }
    
    void grabberStateCb(BoolMsg message) {
        if (!message.data) { // Check if boolmsg is true
            hideGrabber();
            dropObject();
        } else {
            showGrabber();
            grabObject();
        }
    }
}
