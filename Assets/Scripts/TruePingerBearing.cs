using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class truePingerBearing : MonoBehaviour {
	public Transform Diana;
  public Transform pinger1;
	public Transform pinger2;
	public Transform pinger3;
	public Transform pinger4;
	public Transform trueBearing1;
	public Transform trueBearing2;
	public Transform trueBearing3;
	public Transform trueBearing4;
	
	List<Transform> pingersList = new List<Transform>();
	List<Transform> bearingsList = new List<Transform>();

	Quaternion default_rotation = new Quaternion(1.0f, 0f, 0f, 0.0f);

	void Start () {
		pingersList.Add(pinger1);
		pingersList.Add(pinger2);
		pingersList.Add(pinger3);
		pingersList.Add(pinger4);
		bearingsList.Add(trueBearing1);
		bearingsList.Add(trueBearing2);
		bearingsList.Add(trueBearing3);
		bearingsList.Add(trueBearing4);
	}
	
	void SetBearingPosition(Transform bearing, Transform pinger) {
		bearing.position = Diana.position + new Vector3(0, 1 ,0);
		Vector3 lookPos = bearing.position - pinger.position;
		lookPos.y = 0;
		bearing.rotation = Quaternion.LookRotation(lookPos) * default_rotation;
	}

	void Update() {
		for (int i = 0; i < pingersList.Count; i++) {
			SetBearingPosition(bearingsList[i], pingersList[i]);
		}
	}
}