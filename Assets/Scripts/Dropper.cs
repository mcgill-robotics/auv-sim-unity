using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;
using RosMessageTypes.Geometry;

public class Dropper : MonoBehaviour {
    public Transform DroppingSphere;
    ROSConnection roscon;
    private string dropperTopicName = "/actuators/drop";

    void dropSphere(GameObject sphere) {
        // Get the Rigidbody component of the sphere
        Rigidbody rb = sphere.GetComponent<Rigidbody>();

        // Ensure Rigidbody component exists
        if (rb != null) {
            // Make the sphere kinematic
            rb.isKinematic = false; // Set to false to allow gravity to affect the sphere
            rb.detectCollisions = true;

            // Remove the parent (Diana) so it can drop)
            sphere.transform.parent = null;
        }
    }

    void dropSphereCallback(EmptyMsg message) {
        dropSphere(DroppingSphere.gameObject);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<EmptyMsg>("/actuators/drop", dropSphereCallback);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Comma)) {
            dropSphere(DroppingSphere.gameObject);
        }
    }
}