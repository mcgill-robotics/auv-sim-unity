using UnityEngine;

public class FacingChecker : MonoBehaviour {
	public static FacingChecker instance;
	public Transform target; // The object to be faced.
	public Transform auv;
	public int pointsAvailable;
	private float facingThreshold = 0.9f;
	private float requiredFacingTime = 5.0f; // In seconds.

	private float facingTime = 0.0f;

	void Awake() {
		instance = this;
	}

	void Start() {
		this.enabled = false; // Start disabled.
	}

	void Update() {
		if (IsFacingTarget())	{
			facingTime += Time.deltaTime;
			if (facingTime >= requiredFacingTime) {
				PointsManager.instance.AddPoint(pointsAvailable, "Gate");
				this.enabled = false; // Disable the script to stop it from running.
			}
		} else {
			facingTime = 0.0f;
		}
	}

	bool IsFacingTarget() {
		Vector3 directionToTarget = (target.position - auv.position).normalized;
		float dotProduct = Vector3.Dot(auv.forward, directionToTarget);

		return dotProduct >= facingThreshold;
	}

	public void StartScript() {
		this.enabled = true; // Enable the script.
	}

	public void StopScript() {
		this.enabled = false; // Disable the script.
	}
}
