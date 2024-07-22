using UnityEngine;

public class BuoyCircumnavigate : MonoBehaviour
{
	public static BuoyCircumnavigate instance;
	public Transform buoy;
	public Transform auv;
	public int pointsAvailableWrong;
	public int pointsAvailableCorrect;

	private float distanceThreshold = 3f; // In meters.
	private float previousAngle = 0f;
	private float totalRotation = 0f;

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
			totalRotation = 0;
			return;
		}

		Vector3 directionToAuv = auv.position - buoy.position;

		float currentAngle = Mathf.Atan2(directionToAuv.x, directionToAuv.z) * Mathf.Rad2Deg;
		if (currentAngle < 0)
		{
			currentAngle += 360;
		}

		previousAngle = previousAngle == 0 ? currentAngle : previousAngle;
		float angleDifference = currentAngle - previousAngle;
		if (angleDifference < -180)
		{
			angleDifference += 360;
		}
		else if (angleDifference > 180)
		{
			angleDifference -= 360;
		}

		totalRotation += angleDifference;

		// Clockwise = Blue.
		// Anti-clockwise = Red.
		if (totalRotation >= 360)
		{
			if (PointsManager.instance.color == "blue" || PointsManager.instance.color != "red")
			{
				PointsManager.instance.AddPoint(pointsAvailableCorrect, "Buoy");
			}
			else
			{
				PointsManager.instance.AddPoint(pointsAvailableWrong, "Buoy");
			}
			StopScript();
		}
		else if (totalRotation <= -360)
		{
			if (PointsManager.instance.color == "red" || PointsManager.instance.color != "blue")
			{
				PointsManager.instance.AddPoint(pointsAvailableCorrect, "Buoy");
			}
			else
			{
				PointsManager.instance.AddPoint(pointsAvailableWrong, "Buoy");
			}
			StopScript();
		}

		previousAngle = currentAngle;
	}


	public void StartScript()
	{
		this.enabled = true;
	}

	public void StopScript()
	{
		totalRotation = 0;
		this.enabled = false;
	}
}