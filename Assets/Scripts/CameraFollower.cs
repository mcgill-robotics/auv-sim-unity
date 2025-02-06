using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
	public GameObject targetObject;
	public float rigidity = 5f;

	private Vector3 offsetPosition;
	private Quaternion offsetRotation;

	
	private void Awake()
	{
		offsetPosition = targetObject.transform.InverseTransformPoint(transform.position);
		offsetRotation = Quaternion.Inverse(targetObject.transform.rotation) * transform.rotation;
	}

	private void Update()
	{
		Vector3 relativeOffset = targetObject.transform.TransformPoint(offsetPosition);
		transform.position = Vector3.Lerp(transform.position, relativeOffset, Time.deltaTime * rigidity);

		Quaternion relativeRotation = targetObject.transform.rotation * offsetRotation;
		transform.rotation = Quaternion.Slerp(transform.rotation, relativeRotation, Time.deltaTime * rigidity);
	}
}