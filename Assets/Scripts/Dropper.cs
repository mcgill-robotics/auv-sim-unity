using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;
using RosMessageTypes.Geometry;

public class Dropper : MonoBehaviour
{
	public Transform DroppingSphere;

	private ROSConnection roscon;
	private string dropperTopicName = "/actuators/grabber/close";

	void Start()
	{
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.Subscribe<BoolMsg>(dropperTopicName, dropSphereCallback);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Comma))
		{
			dropSphere(DroppingSphere.gameObject);
		}
	}

	void dropSphere(GameObject sphere)
	{
		// Get the Rigidbody component of the sphere
		Rigidbody rb = sphere.GetComponent<Rigidbody>();

		// Ensure Rigidbody component exists
		if (rb != null)
		{
			// Make the sphere kinematic
			rb.isKinematic = false; // Set to false to allow gravity to affect the sphere
			rb.detectCollisions = true;

			// Remove the parent (Diana) so it can drop
			sphere.transform.parent = null;
		}
	}

	void dropSphereCallback(BoolMsg message)
	{
		if (message.data)
		{ // Check if boolmsg is true
			dropSphere(DroppingSphere.gameObject);
		}
	}
}
