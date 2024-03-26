using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class PingerTimeDifference : MonoBehaviour {
    ROSConnection roscon;

    private string pingerTimeDifferenceTopicName = "/sensors/hydrophones/pinger_time_difference";
    private PingerTimeDifferenceMsg timeDiffMsg = new PingerTimeDifferenceMsg();
    private double speedOfSound = 1480.0;

    public Transform hydrophoneO;
    public Transform hydrophoneX;
    public Transform hydrophoneY;
    public Transform hydrophoneZ;
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;
    

    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;

    double[] dO = new double[4];
    double[] dX = new double[4];
    double[] dY = new double[4];
    double[] dZ = new double[4];    // for fourth hydrophone

    double[] timeO = new double[4];
    double[] timeX = new double[4];
    double[] timeY = new double[4];
    double[] timeZ = new double[4]; 
    double[] timeXDiff = new double[4];
    double[] timeYDiff = new double[4];
    double[] timeZDiff = new double[4]; 

    void Start() {
        // hydrophoneO.position = transform.position +  Vector3.up * -0.5f;
        // hydrophoneX.position = hydrophoneO.position + Vector3.forward * 0.1f;
        // hydrophoneY.position = hydrophoneO.position + Vector3.right * -0.1f;
        // hydrophoneZ.position =  hydrophoneO.position + Vector3.up * -0.1f;   // Position unsure, change if incorrect

        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<PingerTimeDifferenceMsg>(pingerTimeDifferenceTopicName); 
    }

    void calculateTimeDifference2D() {
        timeDiffMsg.is_pinger1_active = true;
        timeDiffMsg.is_pinger2_active = true;
        timeDiffMsg.is_pinger3_active = true;
        timeDiffMsg.is_pinger4_active = true;
        
        // Pinger 1
        dO[0] = Vector3.Distance(hydrophoneO.position, pinger1.position);
        dX[0] = Vector3.Distance(hydrophoneX.position, pinger1.position);
        dY[0] = Vector3.Distance(hydrophoneY.position, pinger1.position);

        timeO[0] = dO[0] / speedOfSound;
        timeX[0] = dX[0] / speedOfSound;
        timeY[0] = dY[0] / speedOfSound;

        timeXDiff[0] = timeO[0] - timeX[0];
        timeYDiff[0] = timeO[0] - timeY[0];

        // Pinger 2
        dO[1] = Vector3.Distance(hydrophoneO.position, pinger2.position);
        dX[1] = Vector3.Distance(hydrophoneX.position, pinger2.position);
        dY[1] = Vector3.Distance(hydrophoneY.position, pinger2.position);

        timeO[1] = dO[1] / speedOfSound;
        timeX[1] = dX[1] / speedOfSound;
        timeY[1] = dY[1] / speedOfSound;

        timeXDiff[1] = timeO[1] - timeX[1];
        timeYDiff[1] = timeO[1] - timeY[1];

        // Pinger 3
        dO[2] = Vector3.Distance(hydrophoneO.position, pinger3.position);
        dX[2] = Vector3.Distance(hydrophoneX.position, pinger3.position);
        dY[2] = Vector3.Distance(hydrophoneY.position, pinger3.position);

        timeO[2] = dO[2] / speedOfSound;
        timeX[2] = dX[2] / speedOfSound;
        timeY[2] = dY[2] / speedOfSound;

        timeXDiff[2] =  timeO[2] - timeX[2];
        timeYDiff[2] =  timeO[2] - timeY[2];

        // Pinger 4
        dO[3] = Vector3.Distance(hydrophoneO.position, pinger4.position);
        dX[3] = Vector3.Distance(hydrophoneX.position, pinger4.position);
        dY[3] = Vector3.Distance(hydrophoneY.position, pinger4.position);

        timeO[3] = dO[3] / speedOfSound;
        timeX[3] = dX[3] / speedOfSound;
        timeY[3] = dY[3] / speedOfSound;

        timeXDiff[3] = timeO[3] - timeX[3];
        timeYDiff[3] = timeO[3] - timeY[3];
        
        timeDiffMsg.dt_pinger1 = new double[2] {timeXDiff[0], timeYDiff[0]};
        timeDiffMsg.dt_pinger2 = new double[2] {timeXDiff[1], timeYDiff[1]};
        timeDiffMsg.dt_pinger3 = new double[2] {timeXDiff[2], timeYDiff[2]};
        timeDiffMsg.dt_pinger4 = new double[2] {timeXDiff[3], timeYDiff[3]};

        roscon.Publish(pingerTimeDifferenceTopicName, timeDiffMsg);
    }

    void calculateTimeDifference3D() {
        timeDiffMsg.is_pinger1_active = true;
        timeDiffMsg.is_pinger2_active = true;
        timeDiffMsg.is_pinger3_active = true;
        timeDiffMsg.is_pinger4_active = true;
        
        // Pinger 1
        dO[0] = Vector3.Distance(hydrophoneO.position, pinger1.position);
        dX[0] = Vector3.Distance(hydrophoneX.position, pinger1.position);
        dY[0] = Vector3.Distance(hydrophoneY.position, pinger1.position);
        dZ[0] = Vector3.Distance(hydrophoneZ.position, pinger1.position);

        timeO[0] = dO[0] / speedOfSound;
        timeX[0] = dX[0] / speedOfSound;
        timeY[0] = dY[0] / speedOfSound;
        timeZ[0] = dZ[0] / speedOfSound;

        timeXDiff[0] = timeO[0] - timeX[0];
        timeYDiff[0] = timeO[0] - timeY[0];
        timeZDiff[0] = timeO[0] - timeZ[0];

        // Pinger 2
        dO[1] = Vector3.Distance(hydrophoneO.position, pinger2.position);
        dX[1] = Vector3.Distance(hydrophoneX.position, pinger2.position);
        dY[1] = Vector3.Distance(hydrophoneY.position, pinger2.position);
        dZ[1] = Vector3.Distance(hydrophoneZ.position, pinger2.position);

        timeO[1] = dO[1] / speedOfSound;
        timeX[1] = dX[1] / speedOfSound;
        timeY[1] = dY[1] / speedOfSound;
        timeZ[1] = dZ[1] / speedOfSound;

        timeXDiff[1] = timeO[1] - timeX[1];
        timeYDiff[1] = timeO[1] - timeY[1];
        timeZDiff[1] = timeO[1] - timeZ[1];

        // Pinger 3
        dO[2] = Vector3.Distance(hydrophoneO.position, pinger3.position);
        dX[2] = Vector3.Distance(hydrophoneX.position, pinger3.position);
        dY[2] = Vector3.Distance(hydrophoneY.position, pinger3.position);
        dZ[2] = Vector3.Distance(hydrophoneZ.position, pinger3.position);

        timeO[2] = dO[2] / speedOfSound;
        timeX[2] = dX[2] / speedOfSound;
        timeY[2] = dY[2] / speedOfSound;
        timeZ[2] = dZ[2] / speedOfSound;

        timeXDiff[2] =  timeO[2] - timeX[2];
        timeYDiff[2] =  timeO[2] - timeY[2];
        timeZDiff[2] =  timeO[2] - timeZ[2];

        // Pinger 4
        dO[3] = Vector3.Distance(hydrophoneO.position, pinger4.position);
        dX[3] = Vector3.Distance(hydrophoneX.position, pinger4.position);
        dY[3] = Vector3.Distance(hydrophoneY.position, pinger4.position);
        dZ[3] = Vector3.Distance(hydrophoneZ.position, pinger4.position);

        timeO[3] = dO[3] / speedOfSound;
        timeX[3] = dX[3] / speedOfSound;
        timeY[3] = dY[3] / speedOfSound;
        timeZ[3] = dZ[3] / speedOfSound;

        timeXDiff[3] = timeO[3] - timeX[3];
        timeYDiff[3] = timeO[3] - timeY[3];
        timeZDiff[3] = timeO[3] - timeZ[3];
        

        timeDiffMsg.dt_pinger1 = new double[3] {timeXDiff[0], timeYDiff[0], timeZDiff[0]};
        timeDiffMsg.dt_pinger2 = new double[3] {timeXDiff[1], timeYDiff[1], timeZDiff[1]};
        timeDiffMsg.dt_pinger3 = new double[3] {timeXDiff[2], timeYDiff[2], timeZDiff[2]};
        timeDiffMsg.dt_pinger4 = new double[3] {timeXDiff[3], timeYDiff[3], timeZDiff[3]};

        roscon.Publish(pingerTimeDifferenceTopicName, timeDiffMsg);
    }

    void Update() {
        if (hydrophoneZ == null) {
            calculateTimeDifference2D();
        }
        calculateTimeDifference3D();
    }
}