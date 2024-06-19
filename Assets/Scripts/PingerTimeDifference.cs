using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PingerTimeDifference : MonoBehaviour {    
	double speedOfSound = 1480.0;

	LogicManager1 logicManager1;

	public Transform hydrophoneO;
	public Transform hydrophoneX;
	public Transform hydrophoneY;
	public Transform hydrophoneZ;   // for fourth hydrophone
	public Transform pinger1;
	public Transform pinger2;
	public Transform pinger3;
	public Transform pinger4;
	public int[] frequencies = {1, 2, 3, 4};

	List<Transform> hydrophoneList = new List<Transform>();
	List<Transform> pingersList = new List<Transform>();
	
	void Start() {
		logicManager1 = FindObjectOfType<LogicManager1>(); // Find an instance of the other class
		if (logicManager1 == null) {
			Debug.LogError("[in PingerTimeDifference.cs] LogiManager class is not assigned.");
		}

		hydrophoneList.Add(hydrophoneO);
		hydrophoneList.Add(hydrophoneX);
		hydrophoneList.Add(hydrophoneY);
		hydrophoneList.Add(hydrophoneZ);
		pingersList.Add(pinger1);
		pingersList.Add(pinger2);
		pingersList.Add(pinger3);
		pingersList.Add(pinger4);
	}

	public (uint[], int) calculateTimeDifference(int pinger_index) {   
		double[] hydrophoneSignalTimes = new double[4];  
		double[] hydrophoneToPingerDistances = new double[4];

		for (int i = 0; i < hydrophoneToPingerDistances.Length; i++) {
			hydrophoneToPingerDistances[i] = Vector3.Distance(hydrophoneList[i].position, pingersList[pinger_index].position);
			hydrophoneSignalTimes[i] = hydrophoneToPingerDistances[i] / speedOfSound;
		}   
		
		// Time difference between Hydrophone 0 and all others.
		double[] hydrophonesTimeDifference = new double[3]; 
		for (int i = 0; i < hydrophonesTimeDifference.Length - 1; i++) {
			hydrophonesTimeDifference[i] = hydrophoneSignalTimes[0] - hydrophoneSignalTimes[i + 1];
		}

		uint[] timeInNanoseconds = new uint[3];
		// @TODO - FIX MULTIPLICATION VALUE
		for (int i = 0; i < hydrophonesTimeDifference.Length; i++) { 
			timeInNanoseconds[i] = (uint)(hydrophonesTimeDifference[i] * 100000);
		}
			
		return (timeInNanoseconds[..(logicManager1.hydrophonesNumberOption+2)], frequencies[pinger_index]);
	}
}