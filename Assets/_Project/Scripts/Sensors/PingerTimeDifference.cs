using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PingerTimeDifference : MonoBehaviour
{
    public Transform hydrophone1;
    public Transform hydrophone2;
    public Transform hydrophone3;
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

        hydrophoneList.Add(hydrophone1);
        hydrophoneList.Add(hydrophone2);
        hydrophoneList.Add(hydrophone3);
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

        // Ensure scaledTimes is large enough (max 4 hydrophones)
        if (scaledTimes.Length != pingerToHydrophonesTime.Length)
        {
            scaledTimes = new uint[pingerToHydrophonesTime.Length];
        }

        int hydrophonesOption = SimulationSettings.Instance != null ? SimulationSettings.Instance.HydrophonesNumberOption : 0;
        int count = hydrophonesOption + 3; // 0->3, 1->4

        // Populate reused array
        for (int i = 0; i < count && i < pingerToHydrophonesTime.Length; i++)
        {
            double hydrophoneTime = currentTime + (pingerToHydrophonesTime[i] - minDeltaTime);
            scaledTimes[i] = (uint)((hydrophoneTime * scaledTimeUnit) % doubleUintMaxValue);
        }
        
        // Return the reused array directly. The consumer should respect the known count (3 or 4)
        // Note: This still allocates a tuple, but avoids the array allocation and slicing copy.
        // Ideally consumer would accept a buffer, but this is a significant improvement.
        return (scaledTimes, frequencies[pingerIndex]);
    }
}