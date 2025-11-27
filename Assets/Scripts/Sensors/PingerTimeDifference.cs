using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PingerTimeDifference : MonoBehaviour
{
    public Transform hydrophoneO;
    public Transform hydrophoneX;
    public Transform hydrophoneY;
    public Transform hydrophoneZ;   // for fourth hydrophone
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;
    public int[] frequencies = new int[4];

    private double speedOfSound = 1480.0;
    
    private List<Transform> hydrophoneList = new List<Transform>();
    private List<Transform> pingersList = new List<Transform>();

    private double[] pingerToHydrophonesTime = new double[4];
    private double[] hydrophoneToPingerDistances = new double[4];
    private double currentTime; // in seconds.
    private int scaledTimeUnit = 10_000_000; // 10e-7 --> 1 second = 10,000,000
    private uint uintMaxValue = uint.MaxValue;
    private double doubleUintMaxValue;
    private uint[] scaledTimes = new uint[3];

    private void Start()
    {
        doubleUintMaxValue = (double)uintMaxValue;

        hydrophoneList.Add(hydrophoneO);
        hydrophoneList.Add(hydrophoneX);
        hydrophoneList.Add(hydrophoneY);
        hydrophoneList.Add(hydrophoneZ);
        pingersList.Add(pinger1);
        pingersList.Add(pinger2);
        pingersList.Add(pinger3);
        pingersList.Add(pinger4);
    }

    /// <summary>
    /// Simulate the absolute time that each hydrophones detected a signal.
    /// </summary>
    /// <param name="pingerIndex">The pinger to calculate the time difference for.</param>
    /// <returns>Absolute time for each hydrophone of when it detected signal, frequency of signal.</returns>
    public (uint[], int) CalculateTimeDifference(int pingerIndex)
    {
        currentTime = Time.time;

        double minDeltaTime = double.MaxValue;
        for (int i = 0; i < hydrophoneToPingerDistances.Length; i++)
        {
            hydrophoneToPingerDistances[i] = Vector3.Distance(hydrophoneList[i].position, pingersList[pingerIndex].position);
            pingerToHydrophonesTime[i] = hydrophoneToPingerDistances[i] / speedOfSound;
            if (pingerToHydrophonesTime[i] < minDeltaTime)
            {
                minDeltaTime = pingerToHydrophonesTime[i];
            }
        }

        scaledTimes = new uint[pingerToHydrophonesTime.Length];
        for (int i = 0; i < pingerToHydrophonesTime.Length; i++)
        {
            double hydrophoneTime = currentTime + (pingerToHydrophonesTime[i] - minDeltaTime);
            scaledTimes[i] = (uint)((hydrophoneTime * scaledTimeUnit) % doubleUintMaxValue);
        }
        
        int hydrophonesOption = SimulationSettings.Instance != null ? SimulationSettings.Instance.HydrophonesNumberOption : 0;
        return (scaledTimes[..(hydrophonesOption + 3)], frequencies[pingerIndex]);
    }
}