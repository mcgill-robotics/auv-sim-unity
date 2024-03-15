using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class Dropper : MonoBehaviour {
    ROSConnection roscon;
    private string DropperTopicName = "dropper";
    public Transform Diana; // Assuming this is the object from where the sphere should be dropped
    public GameObject SpherePrefab; // Prefab of the sphere to drop

    void dropSphere(PingerBearingMsg msg) {
        // Instantiate the sphere
        GameObject sphere = Instantiate(SpherePrefab, Diana.position, Quaternion.identity);

        // Make the sphere kinematic
        Rigidbody rb = sphere.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.detectCollisions = false;


        // Remove the parent (diana) so it can drop)
        if (sphere.transform.parent != null)
            sphere.transform.parent = null;
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<PingerBearingMsg>(DropperTopicName, dropSphere);
    }
}
