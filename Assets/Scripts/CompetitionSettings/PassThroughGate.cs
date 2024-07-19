using System;
using UnityEngine;
using System.Collections;

public class PassThroughGate : MonoBehaviour {
	public static PassThroughGate instance;
	public Transform gate;
	public Transform gatePole1;
	public Transform gatePole2;
	public Transform auv;
	public int pointsAvailable;
	private float gateHeight = 1.5f;
	private bool hasEnteredGate = false;

	void Awake() {
		instance = this;
	}

	void Start() {
		this.enabled = false; // Start disabled.
	}
	
	void Update() {
		CheckGatePassage();
	}

	void CheckGatePassage() {
		if (IsWithinGateBounds()) {
			if (!hasEnteredGate) {
				hasEnteredGate = true;
			}
		}	else {
			if (hasEnteredGate) {
				hasEnteredGate = false;
				PointsManager.instance.AddPoint(pointsAvailable, "Gate");
				this.enabled = false;
			}
		}
	}

	bool IsWithinGateBounds() {
		// Gate as rectangle (2D) that you have to go through.
		Vector3 auvPosition = auv.position;
		Vector3 gatePosition = gate.position;
		Vector3 pole1Position = gatePole1.position;
		Vector3 pole2Position = gatePole2.position;

		// Z axis check (unity).
		bool withinZBounds = auvPosition.z <= Math.Max(pole1Position.z, pole2Position.z) && auvPosition.z >= Math.Min(pole1Position.z, pole2Position.z);

		// X axis check (unity).
		bool withinXBounds = auvPosition.x <= Math.Max(pole1Position.x, pole2Position.x) && auvPosition.x >= Math.Min(pole1Position.x, pole2Position.x);

		// Y axis check (unity).
		float topGate = gatePosition.y + (gateHeight / 2);
		float bottomGate = gatePosition.y - (gateHeight / 2);
		bool withinYBounds = auvPosition.y <= topGate;		

		return withinZBounds && withinXBounds && withinYBounds;
	}

	public void StartScript() {
		this.enabled = true; // Enable the script.
	}

	public void StopScript() {
		this.enabled = false; // Disable the script.
	}
}
