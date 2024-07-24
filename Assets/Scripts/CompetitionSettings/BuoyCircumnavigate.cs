using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BuoyCircumnavigate : MonoBehaviour
{
	public static BuoyCircumnavigate instance;
	public Transform buoy;
	public Transform auv;
	public int pointsAvailableWrong;
	public int pointsAvailableCorrect;

	private float distanceThreshold = 3f; // In meters.
	private bool isFirst = true;
	private Vector3 initialPosition;
	// {(+x,+z), (-x,-z), (+x,-z), (-x,+z)} auv's position relative to buoy's.
	private List<bool> checkPoints = new List<bool>() { false, false, false, false };
	private int firstCheckPoint;
	private bool isClockWise;
	private bool calculateDirection = true;
	private bool isLastCheckPoint = false;

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
		auvPosition.y = 0;
		Vector3 buoyPosition = buoy.position;
		buoyPosition.y = 0;
		if (Vector3.Distance(auvPosition, buoyPosition) > distanceThreshold)
		{
			ResetCheckPoints();
			isFirst = true;
			return;
		}

		UpdateCheckPoints(auvPosition, buoyPosition);

		if (isFirst)
		{
			initialPosition = auvPosition;
			firstCheckPoint = checkPoints.IndexOf(true);
			isFirst = false;
		}

		if (checkPoints.Count(value => value) == 2 && calculateDirection)
		{
			Vector3 initialVector = initialPosition - buoyPosition;
			Vector3 currentVector = auvPosition - buoyPosition;
			float sign = Mathf.Sign(Vector3.Cross(initialVector, currentVector).y);
			float angle = sign * Vector3.Angle(initialVector, currentVector);
			isClockWise = angle > 0 ? true : false;
			calculateDirection = false;
		}
		else if (checkPoints.Count(value => value) == 4)
		{
			if (!isLastCheckPoint)
			{
				checkPoints[firstCheckPoint] = false;
				isLastCheckPoint = true;
				return;
			}
			CalculatePoints();
			StopScript();
		}
	}

	private void UpdateCheckPoints(Vector3 auvPosition, Vector3 buoyPosition)
	{
		if (auvPosition.x > buoyPosition.x && auvPosition.z > buoyPosition.z)
		{
			checkPoints[0] = true;
		}
		else if (auvPosition.x < buoyPosition.x && auvPosition.z < buoyPosition.z)
		{
			checkPoints[1] = true;
		}
		else if (auvPosition.x > buoyPosition.x && auvPosition.z < buoyPosition.z)
		{
			checkPoints[2] = true;
		}
		else if (auvPosition.x < buoyPosition.x && auvPosition.z > buoyPosition.z)
		{
			checkPoints[3] = true;
		}
	}

	private void CalculatePoints()
	{
		if (isClockWise)
		{
			if (PointsManager.instance.color == "red")
			{
				PointsManager.instance.AddPoint(pointsAvailableCorrect, "Buoy");
				MessageBox.instance.AddMessage("Buoy Circumnavigate correct (clockwise) +800pts");
			}
			else if (PointsManager.instance.color == "blue")
			{
				PointsManager.instance.AddPoint(pointsAvailableWrong, "Buoy");
				MessageBox.instance.AddMessage("Buoy Circumnavigate wrong (counterclockwise) +600pts");
			}
			else
			{
				PointsManager.instance.color = "red";
				PointsManager.instance.AddPoint(pointsAvailableCorrect, "Buoy");
				MessageBox.instance.AddMessage("Buoy Circumnavigate correct (clockwise) +800pts");
				MessageBox.instance.AddMessage("Setting competition color to RED");
			}
		}
		else
		{
			if (PointsManager.instance.color == "blue")
			{
				PointsManager.instance.AddPoint(pointsAvailableCorrect, "Buoy");
				MessageBox.instance.AddMessage("Buoy Circumnavigate correct (counterclockwise) +800pts");
			}
			else if (PointsManager.instance.color == "red")
			{
				PointsManager.instance.AddPoint(pointsAvailableWrong, "Buoy");
				MessageBox.instance.AddMessage("Buoy Circumnavigate wrong (clockwise) +600pts");
			}
			else
			{
				PointsManager.instance.color = "blue";
				PointsManager.instance.AddPoint(pointsAvailableCorrect, "Buoy");
				MessageBox.instance.AddMessage("Buoy Circumnavigate correct (counterclockwise) +800pts");
				MessageBox.instance.AddMessage("Setting competition color to BLUE");
			}
		}
	}

	private void ResetCheckPoints()
	{
		isLastCheckPoint = false;
		for (int i = 0; i < checkPoints.Count; i++)
		{
			checkPoints[i] = false;
		}
	}

	public void StartScript()
	{
		this.enabled = true;
	}

	public void StopScript()
	{
		ResetCheckPoints();
		calculateDirection = true;
		this.enabled = false;
	}
}