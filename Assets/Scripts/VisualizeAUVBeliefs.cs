using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class VisualizeAUVBeliefs : MonoBehaviour
{
    ROSConnection roscon;
    public GameObject dianaVisualization;
    public GameObject laneMarker1Visualization;
    public GameObject laneMarker2Visualization;
    public GameObject gateVisualization;
    public GameObject buoyVisualization;
    public GameObject octagonTableVisualization;
    public GameObject detectionIndicatorPrefab;

    public int maxDetectionFrameIndicators = 50;

    public string thetaXTopicName = "/state/theta/x";
    public string thetaYTopicName = "/state/theta/y";
    public string thetaZTopicName = "/state/theta/z";
    public string posXTopicName = "/state/x";
    public string posYTopicName = "/state/y";
    public string posZTopicName = "/state/z";
    public string objectMapTopicName = "/vision/object_map";
    public string detectionFrameTopicName = "/vision/viewframe_detection";

    private List<GameObject> detectionFrameIndicators;

    private Vector3 currentAUVPos = Vector3.zero;
    private Vector3 currentAUVRot = Vector3.zero;

    void Start() {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<RosMessageTypes.Auv.VisionObjectArrayMsg>(detectionFrameTopicName, detectionFrameCallback);
        roscon.Subscribe<RosMessageTypes.Auv.VisionObjectArrayMsg>(objectMapTopicName, objectMapCallback);
        roscon.Subscribe<Float64Msg>(thetaXTopicName, thetaXCallback);
        roscon.Subscribe<Float64Msg>(thetaYTopicName, thetaYCallback);
        roscon.Subscribe<Float64Msg>(thetaZTopicName, thetaZCallback);
        roscon.Subscribe<Float64Msg>(posXTopicName, posXCallback);
        roscon.Subscribe<Float64Msg>(posYTopicName, posYCallback);
        roscon.Subscribe<Float64Msg>(posZTopicName, posZCallback);
    }

    void thetaXCallback(Float64Msg thetaX) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.z = (float)thetaX.data;
    }
    void thetaYCallback(Float64Msg thetaY) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.x = (float)-thetaY.data;
    }
    void thetaZCallback(Float64Msg thetaZ) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.y = (float)thetaZ.data;
    }
    void posXCallback(Float64Msg X) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVPos.z = (float)X.data;
    }
    void posYCallback(Float64Msg Y) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVPos.x = (float)-Y.data;
    }
    void posZCallback(Float64Msg Z) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVPos.y = (float)Z.data;
    }
    
    void objectMapCallback(RosMessageTypes.Auv.VisionObjectArrayMsg map) {

    }

    void detectionFrameCallback(RosMessageTypes.Auv.VisionObjectArrayMsg detectionFrame) {

    }

    void Update()
    {
        dianaVisualization.transform.position = currentAUVPos;
        dianaVisualization.transform.rotation = Quaternion.Euler(currentAUVRot.x, currentAUVRot.y, currentAUVRot.z);
    }
}
