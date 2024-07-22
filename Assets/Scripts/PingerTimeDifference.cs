using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class PingerTimeDifference : MonoBehaviour
{
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
	public int[] frequencies = new int[4];

	List<Transform> hydrophoneList = new List<Transform>();
	List<Transform> pingersList = new List<Transform>();

	double[] pingerToHydrophonesTime = new double[4];
	double[] hydrophoneToPingerDistances = new double[4];
	double[] hydrophonesTimes = new double[3];
	double currentTime; // in seconds.
	int scaledTimeUnit = 10000000; // 10e-7 --> 1 second = 10,000,000
	uint uintMaxValue = uint.MaxValue;
	double doubleUintMaxValue;
	uint[] scaledTimes = new uint[3];

	void Start()
	{
		doubleUintMaxValue = (double)uintMaxValue;

		logicManager1 = FindObjectOfType<LogicManager1>(); // Find an instance of the other class.
		if (logicManager1 == null)
		{
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

	public (uint[], int) CalculateTimeDifference(int pinger_index)
	{
		/* 
		Simulate absolute the time that each hydrophones detected a signal.
		Returns: (Absolute time for eachc hydrophone of when it detected signal, frequency of signal)
		*/

		currentTime = Time.time;

		// 1. Calculate  the time it takes for frequency wave to get to each hydrophone. 
		// Delta_time = Delta_space / Velocity_of_sound
		for (int i = 0; i < hydrophoneToPingerDistances.Length; i++)
		{
			hydrophoneToPingerDistances[i] = Vector3.Distance(hydrophoneList[i].position, pingersList[pinger_index].position);
			pingerToHydrophonesTime[i] = hydrophoneToPingerDistances[i] / speedOfSound;
		}

		// 2. Get min Delta_time and calculate the difference to the other Delta_time.
		double minDeltaTime = pingerToHydrophonesTime.Min();
		double[] differences = pingerToHydrophonesTime.Select(value => value - minDeltaTime).ToArray();

		// 3. Add the difference (always positive) to the currentTime --> absolute time.
		hydrophonesTimes = differences.Select(diff => diff + currentTime).ToArray();

		// 4. Scale times & 5. Simulate overflow.
		scaledTimes = hydrophonesTimes.Select(time => (uint)((time * scaledTimeUnit) % doubleUintMaxValue)).ToArray();

		return (scaledTimes[..(logicManager1.hydrophonesNumberOption + 3)], frequencies[pinger_index]);
	}
}