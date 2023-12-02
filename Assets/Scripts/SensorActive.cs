using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.UI;

public class SensorActive : MonoBehaviour {
    public StatePublisher statePublisher;
    public Toggle DVLToggle;
    public Toggle IMUToggle;
    public Toggle DepthToggle;
    

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        statePublisher.isDVLActive = DVLToggle.isOn;
        statePublisher.isDepthSensorActive = DepthToggle.isOn;
        statePublisher.isIMUActive = IMUToggle.isOn;
        // debug.log("DVL: " + statePublisher.isDVLActive);
    }
}