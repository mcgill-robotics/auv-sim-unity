using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OctagonSurface : MonoBehaviour
{
	public static OctagonSurface instance;
	public Transform auv;
	public Transform[] octagonCylinders;
	public int pointsAvailable;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		this.enabled = false;
	}

	void Update()
	{
		Vector3 auvPosition = auv.position;

		// Not close to the surface.
		if (auvPosition.y < -0.3)
		{
			return;
		}

		int crossingNumber = 0;
		int pointCount = octagonCylinders.Length;

		// Ray-casting algorithm for polygons.
		for (int i = 0; i < pointCount; i++)
		{
			Vector3 vertex1 = octagonCylinders[i].position;
			Vector3 vertex2 = octagonCylinders[(i + 1) % pointCount].position;

			if (((vertex1.z <= auvPosition.z && auvPosition.z < vertex2.z) || (vertex2.z <= auvPosition.z && auvPosition.z < vertex1.z)) &&
					(auvPosition.x < (vertex2.x - vertex1.x) * (auvPosition.z - vertex1.z) / (vertex2.z - vertex1.z) + vertex1.x))
			{
				crossingNumber++;
			}
		}

		if (crossingNumber % 2 != 0)
		{
			PointsManager.instance.AddPoint(pointsAvailable, "Octagon");
			MessageBox.instance.AddMessage(string.Format("Octagon Surface +{0}pts", pointsAvailable));
			StopScript();
		}
	}

	public void StartScript()
	{
		this.enabled = true;
	}

	public void StopScript()
	{
		this.enabled = false;
	}
}