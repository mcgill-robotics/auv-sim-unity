using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class PingerTimeDifference : MonoBehaviour {
    ROSConnection roscon;

    public string pingerTimeDifferenceTopicName = "/sensors/hydrophones/pinger_time_difference";
    
    public double speedOfSound = 1480.0;

    LogicManager1 logicManager1;

    public Transform hydrophoneO;
    public Transform hydrophoneX;
    public Transform hydrophoneY;
    public Transform hydrophoneZ;   // for fourth hydrophone
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;

    public List<Transform> hydrophoneList = new List<Transform>();
    public List<Transform> pingersList = new List<Transform>();

    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;
    int[] frequencies = {1, 2, 3, 4};

    double[] hydrophoneToPingerDistances = new double[4];
    double[] hydrophoneSignalTimes = new double[4];  
    // Time difference between Hydrophone 0 and all others
    double[] hydrophonesTimeDifference = new double[3]; 
    uint[] timeInNanoseconds = new uint[3];

    void Start() {
        // hydrophoneO.position = transform.position +  Vector3.up * -0.5f;
        // hydrophoneX.position = hydrophoneO.position + Vector3.forward * 0.1f;
        // hydrophoneY.position = hydrophoneO.position + Vector3.right * -0.1f;
        // hydrophoneZ.position =  hydrophoneO.position + Vector3.up * -0.1f;   // Position unsure, change if incorrect

        hydrophoneList.Add(hydrophoneO);
        hydrophoneList.Add(hydrophoneX);
        hydrophoneList.Add(hydrophoneY);
        hydrophoneList.Add(hydrophoneZ);
        pingersList.Add(pinger1);
        pingersList.Add(pinger2);
        pingersList.Add(pinger3);
        pingersList.Add(pinger4);

        logicManager1 = FindObjectOfType<LogicManager1>(); // Find an instance of the other class

        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<PingerTimeDifferenceMsg>(pingerTimeDifferenceTopicName);         
    }


    void calculateTimeDifference() {   
        for (int j = 0; j < pingersList.Count; j++) {  
            for (int i = 0; i < hydrophoneToPingerDistances.Length; i++) {
                hydrophoneToPingerDistances[i] = Vector3.Distance(hydrophoneList[i].position, pingersList[j].position);
                hydrophoneSignalTimes[i] = hydrophoneToPingerDistances[i] / speedOfSound;
            }   
            
            for (int i = 0; i < hydrophonesTimeDifference.Length - 1; i++) {
                hydrophonesTimeDifference[i] = hydrophoneSignalTimes[0] - hydrophoneSignalTimes[i + 1];
            }
            // Debug.Log();
            // for (int i = 0; i < hydrophonesTimeDifference.Length; i++) {
            //     // Check if the converted value fits within the uint range (0 to 4294967295)
            //     if (hydrophonesTimeDifference[i] * 10e-8 <= uint.MaxValue) {
            //         // Cast the double value multiplied by 10e-8 to uint
            //         timeInNanoseconds[i] = (uint)(hydrophonesTimeDifference[i] * 10e-8);
            //     } else {
            //         // Handle potential overflow if the value is too large for uint
            //         Debug.LogError($"Time value {hydrophonesTimeDifference[i]} exceeds uint range after conversion. Consider using a different data type.");
            //     }
            // }
            
            // @TODO - FIX MULTIPLICATION VALUE
            for (int i = 0; i < hydrophonesTimeDifference.Length; i++) { 
                timeInNanoseconds[i] = (uint)(hydrophonesTimeDifference[i] * 100000);
            }

            PingerTimeDifferenceMsg timeDiffMsg = new PingerTimeDifferenceMsg();
            timeDiffMsg.frequency = frequencies[j];
            
            timeDiffMsg.times = timeInNanoseconds[..(logicManager1.hydrophonesNumberOption+2)];
            
            roscon.Publish(pingerTimeDifferenceTopicName, timeDiffMsg);
        }
    }

    void Update() {
        calculateTimeDifference();
    }
}