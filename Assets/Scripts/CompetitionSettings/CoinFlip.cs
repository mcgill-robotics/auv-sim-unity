using UnityEngine;

public class CoinFlip : MonoBehaviour
{
	public static CoinFlip instance;
	public Transform gate; // The object to be faced.
	public Transform auv;
	public int pointsAvailable;

	private float yawAngleThreshold = 45f; // In degrees.
	private float rollPitchAngleThreshold = 15f; // In degrees.
	private float intrinsicAuvYaw = 90f;
	private float requiredFacingTime = 5.0f; // In seconds.
	private float distanceThreshold = 3f; // In meters.

	private float facingTimer = 0;

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
		float distanceToGate = Vector3.Distance(gate.position, auv.position);
		Vector3 auvRotation = auv.rotation.eulerAngles;

		if (distanceToGate > distanceThreshold ||
			Mathf.Abs(auvRotation.x) > rollPitchAngleThreshold ||
			Mathf.Abs(auvRotation.z) > rollPitchAngleThreshold)
		{
			facingTimer = 0f;
			return;
		}

		Vector3 targetDirection = gate.position - auv.position;
		float targetYaw = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

		float curYaw = auvRotation.y > 180 ? auvRotation.y - 360 : auvRotation.y;
		curYaw += intrinsicAuvYaw;

		if (Mathf.Abs(targetYaw - curYaw) <= yawAngleThreshold)
		{
			facingTimer += Time.deltaTime;

			if (facingTimer >= requiredFacingTime)
			{
				PointsManager.instance.AddPoint(pointsAvailable, "Gate");
				MessageBox.instance.AddMessage("Coin Flip +300pts");
				StopScript();
			}
		}
		else
		{
			facingTimer = 0.0f;
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
