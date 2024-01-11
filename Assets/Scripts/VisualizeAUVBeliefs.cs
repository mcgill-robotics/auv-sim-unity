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
    public GameObject laneMarker1FromVisualization;
    public GameObject laneMarker1ToVisualization;
    public GameObject laneMarker2FromVisualization;
    public GameObject laneMarker2ToVisualization;
    public GameObject gateVisualization;
    public GameObject buoyVisualization;
    public GameObject octagonTableVisualization;
    public GameObject abydosSymbol1Visualization;
    public GameObject abydosSymbol2Visualization;
    public GameObject earthSymbol1Visualization;
    public GameObject earthSymbol2Visualization;
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

    private List<GameObject> detectionFrameIndicators = new List<GameObject>();


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
        currentAUVRot.z = (float)-thetaX.data;
    }
    void thetaYCallback(Float64Msg thetaY) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.x = (float)thetaY.data;
    }
    void thetaZCallback(Float64Msg thetaZ) {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.y = (float)-thetaZ.data;
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
        int num_lane_markers_in_map = 0;
        int num_abydos_symbols_in_map = 0;
        int num_earth_symbols_in_map = 0;
        bool buoyInMap = false;
        bool gateInMap = false;
        bool octagonTableInMap = false;
        foreach (RosMessageTypes.Auv.VisionObjectMsg detection in map.array)
        {
            if (detection.label == "Lane Marker") {
                if (num_lane_markers_in_map == 0) {
                    laneMarker1FromVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    laneMarker1ToVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    laneMarker1FromVisualization.transform.rotation = Quaternion.Euler(-90, (float)-detection.theta_z, 0) * Quaternion.Euler(0,0,90);
                    laneMarker1ToVisualization.transform.rotation = Quaternion.Euler(-90, (float)-detection.extra_field, 0) * Quaternion.Euler(0,0,90);
                } else {
                    laneMarker2FromVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    laneMarker2ToVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    laneMarker2FromVisualization.transform.rotation = Quaternion.Euler(-90, (float)-detection.theta_z, 0) * Quaternion.Euler(0,0,90);
                    laneMarker2ToVisualization.transform.rotation = Quaternion.Euler(-90, (float)-detection.extra_field, 0) * Quaternion.Euler(0,0,90);
                }
                num_lane_markers_in_map += 1;
            } else if (detection.label == "Gate") {
                gateInMap = true;
                gateVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                gateVisualization.transform.rotation = Quaternion.Euler(0, (float)-detection.theta_z, 0);
                if (detection.extra_field > 0.5) gateVisualization.transform.localScale = new Vector3(-1, 1, 1);
            } else if (detection.label == "Buoy") {
                buoyInMap = true;
                buoyVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                buoyVisualization.transform.rotation = Quaternion.Euler(0, (float)detection.theta_z, 0);
            } else if (detection.label == "Octagon Table") {
                octagonTableInMap = true;
                octagonTableVisualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                octagonTableVisualization.transform.rotation = Quaternion.Euler(0, (float)-detection.theta_z, 0);
            } else if (detection.label == "Abydos Symbol") {
                if (num_abydos_symbols_in_map == 0) {
                    abydosSymbol1Visualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    abydosSymbol1Visualization.transform.rotation = Quaternion.Euler(0, (float)-detection.theta_z, 0);
                } else {
                    abydosSymbol2Visualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    abydosSymbol2Visualization.transform.rotation = Quaternion.Euler(0, (float)-detection.extra_field, 0);
                }
                num_abydos_symbols_in_map += 1;
            } else if (detection.label == "Earth Symbol") {
                if (num_earth_symbols_in_map == 0) {
                    earthSymbol1Visualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    earthSymbol1Visualization.transform.rotation = Quaternion.Euler(0, (float)-detection.theta_z, 0);
                } else {
                    earthSymbol2Visualization.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
                    earthSymbol2Visualization.transform.rotation = Quaternion.Euler(0, (float)-detection.extra_field, 0);
                }
                num_earth_symbols_in_map += 1;
            }

            laneMarker1FromVisualization.SetActive(num_lane_markers_in_map > 0);
            laneMarker1ToVisualization.SetActive(num_lane_markers_in_map > 0);
            laneMarker2FromVisualization.SetActive(num_lane_markers_in_map > 1);
            laneMarker2ToVisualization.SetActive(num_lane_markers_in_map > 1);
            buoyVisualization.SetActive(buoyInMap);
            gateVisualization.SetActive(gateInMap);
            octagonTableVisualization.SetActive(octagonTableInMap);
            abydosSymbol1Visualization.SetActive(num_abydos_symbols_in_map > 0);
            abydosSymbol2Visualization.SetActive(num_abydos_symbols_in_map > 1);
            earthSymbol1Visualization.SetActive(num_earth_symbols_in_map > 0);
            earthSymbol2Visualization.SetActive(num_earth_symbols_in_map > 1);
        }
    }

    void detectionFrameCallback(RosMessageTypes.Auv.VisionObjectArrayMsg detectionFrame) {
        foreach (RosMessageTypes.Auv.VisionObjectMsg detection in detectionFrame.array)
        {
            GameObject detectionObject;
            if (detectionFrameIndicators.Count >= maxDetectionFrameIndicators) {
                detectionObject = detectionFrameIndicators[0];
                detectionFrameIndicators.RemoveAt(0);
            } else {
                detectionObject = GameObject.Instantiate(detectionIndicatorPrefab);
            }
            detectionObject.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
        }
    }

    void Update()
    {
        dianaVisualization.transform.position = currentAUVPos;
        dianaVisualization.transform.rotation = Quaternion.Euler(currentAUVRot.x, currentAUVRot.y, currentAUVRot.z) * Quaternion.Euler(0,-90,0);
    }
}
