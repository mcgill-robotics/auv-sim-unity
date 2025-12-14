using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAUVDepth : MonoBehaviour
{
	public Transform auv;

	private void Update()
	{
		Vector3 pos = transform.position;
		pos.y = auv.transform.position.y;
		transform.position = pos;
	}
}