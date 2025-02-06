using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class Dropper : MonoBehaviour
{
	public Rigidbody DroppingSphereRb;

	private ROSConnection roscon;
	private string dropperTopicName = "/actuators/grabber/close";

	
	private void Start()
	{
		// Start the ROS connection
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.Subscribe<BoolMsg>(dropperTopicName, DropSphereCallback);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Comma))
		{
			DropSphere();
		}
	}

	private void DropSphere()
	{
		// Ensure Rigidbody component exists
		if (DroppingSphereRb == null) return;
		
		// Make the sphere kinematic
		DroppingSphereRb.isKinematic = false; // Set to false to allow gravity to affect the sphere
		DroppingSphereRb.detectCollisions = true;

		// Remove the parent (Diana) so it can drop
		DroppingSphereRb.transform.parent = null;
	}

	private void DropSphereCallback(BoolMsg message)
	{
		// If the "/actuators/grabber/close" message is true then drop the sphere
		if (message.data)
		{
			DropSphere();
		}
	}
}
