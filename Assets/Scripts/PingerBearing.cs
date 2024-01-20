using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;

public class PingerBearing : MonoBehaviour {
    ROSConnection roscon;

    private string pingerTimeDifferenceTopicName = "/sensors/hydrophones/pinger_time_difference";
    private PingerTimeDifferenceMsg timeDiffMsg = new PingerTimeDifferenceMsg();
    private double speedOfSound = 1480.0;
    
    public Transform Diana;

    public Transform hydrophone1;
    public Transform hydrophone2;
    public Transform hydrophone3;
    public Transform pinger1;
    public Transform pinger2;
    public Transform pinger3;
    public Transform pinger4;
    
    Vector3 pinger1Bearing;
    Vector3 pinger2Bearing;
    Vector3 pinger3Bearing;
    Vector3 pinger4Bearing;

    double[] d1 = new double[4];
    double[] d2 = new double[4];
    double[] d3 = new double[4];

    double[] time1 = new double[4];
    double[] time2 = new double[4];
    double[] time3 = new double[4];
    double[] time2Diff = new double[4];
    double[] time3Diff = new double[4];

    void calculateTimeDifference() {
        timeDiffMsg.is_pinger1_active = true;
        timeDiffMsg.is_pinger2_active = true;
        timeDiffMsg.is_pinger3_active = true;
        timeDiffMsg.is_pinger4_active = true;
        
        // Pinger 1
        d1[0] = Vector3.Distance(hydrophone1.position, pinger1.position);
        d2[0] = Vector3.Distance(hydrophone2.position, pinger1.position);
        d3[0] = Vector3.Distance(hydrophone3.position, pinger1.position);

        time1[0] = d1[0] / speedOfSound;
        time2[0] = d2[0] / speedOfSound;
        time3[0] = d3[0] / speedOfSound;

        time2Diff[0] = time2[0] - time1[0];
        time3Diff[0] = time3[0] - time1[0];

        // Pinger 2
        d1[1] = Vector3.Distance(hydrophone1.position, pinger2.position);
        d2[1] = Vector3.Distance(hydrophone2.position, pinger2.position);
        d3[1] = Vector3.Distance(hydrophone3.position, pinger2.position);

        time1[1] = d1[1] / speedOfSound;
        time2[1] = d2[1] / speedOfSound;
        time3[1] = d3[1] / speedOfSound;

        time2Diff[1] = time2[1] - time1[1];
        time3Diff[1] = time3[1] - time1[1];

        // Pinger 3
        d1[2] = Vector3.Distance(hydrophone1.position, pinger3.position);
        d2[2] = Vector3.Distance(hydrophone2.position, pinger3.position);
        d3[2] = Vector3.Distance(hydrophone3.position, pinger3.position);

        time1[2] = d1[2] / speedOfSound;
        time2[2] = d2[2] / speedOfSound;
        time3[2] = d3[2] / speedOfSound;

        time2Diff[2] = time2[2] - time1[2];
        time3Diff[2] = time3[2] - time1[2];

        // Pinger 4
        d1[3] = Vector3.Distance(hydrophone1.position, pinger4.position);
        d2[3] = Vector3.Distance(hydrophone2.position, pinger4.position);
        d3[3] = Vector3.Distance(hydrophone3.position, pinger4.position);

        time1[3] = d1[3] / speedOfSound;
        time2[3] = d2[3] / speedOfSound;
        time3[3] = d3[3] / speedOfSound;

        time2Diff[3] = time2[3] - time1[3];
        time3Diff[3] = time3[3] - time1[3];
        

        timeDiffMsg.dt_pinger1 = new double[2] {time2Diff[0], time3Diff[0]};
        timeDiffMsg.dt_pinger2 = new double[2] {time2Diff[1], time3Diff[1]};
        timeDiffMsg.dt_pinger3 = new double[2] {time2Diff[2], time3Diff[2]};
        timeDiffMsg.dt_pinger4 = new double[2] {time2Diff[3], time3Diff[3]};

        roscon.Publish(pingerTimeDifferenceTopicName, timeDiffMsg);
    }

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<PingerTimeDifferenceMsg>(pingerTimeDifferenceTopicName); 
    }

    void Update() {
        calculateTimeDifference();
    }
}