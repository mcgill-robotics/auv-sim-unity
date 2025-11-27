using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class VisualizeAUVBeliefs : MonoBehaviour
{
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
    public GameObject trueBearingPinger1;
    public GameObject trueBearingPinger2;
    public GameObject trueBearingPinger3;
    public GameObject trueBearingPinger4;
    public GameObject expectedBearingPinger1;
    public GameObject expectedBearingPinger2;
    public GameObject expectedBearingPinger3;
    public GameObject expectedBearingPinger4;
    public GameObject binVisualization;

    public int maxDetectionFrameIndicators = 10;
    private int currentIndex = 0;

    private ROSConnection roscon;


    private List<GameObject> detectionFrameIndicators = new List<GameObject>();
    
    private Vector3 currentAUVPos = Vector3.zero;
    private Vector3 currentAUVRot = Vector3.zero;

    private void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<VisionObjectArrayMsg>(ROSSettings.Instance.VisionDetectionFrameTopic, detectionFrameCallback);
        roscon.Subscribe<VisionObjectArrayMsg>(ROSSettings.Instance.VisionObjectMapTopic, objectMapCallback);
        roscon.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaXTopic, thetaXCallback);
        roscon.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaYTopic, thetaYCallback);
        roscon.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaZTopic, thetaZCallback);
        roscon.Subscribe<Float64Msg>(ROSSettings.Instance.StateXTopic, posXCallback);
        roscon.Subscribe<Float64Msg>(ROSSettings.Instance.StateYTopic, posYCallback);
        roscon.Subscribe<Float64Msg>(ROSSettings.Instance.StateZTopic, posZCallback);
    }

    private void Update()
    {
        dianaVisualization.transform.position = currentAUVPos;
        dianaVisualization.transform.rotation = Quaternion.Euler(
            currentAUVRot.x * Mathf.Rad2Deg, 
            currentAUVRot.y * Mathf.Rad2Deg, 
            currentAUVRot.z * Mathf.Rad2Deg
        ) * Quaternion.Euler(0f, -90f, 0f);

        UpdateBearingVisualizations();
    }

    private void UpdateBearingVisualizations()
    {
        if (SimulationSettings.Instance == null) return;

        SetBearingActive(trueBearingPinger1, expectedBearingPinger1, SimulationSettings.Instance.VisualizeBearing1);
        SetBearingActive(trueBearingPinger2, expectedBearingPinger2, SimulationSettings.Instance.VisualizeBearing2);
        SetBearingActive(trueBearingPinger3, expectedBearingPinger3, SimulationSettings.Instance.VisualizeBearing3);
        SetBearingActive(trueBearingPinger4, expectedBearingPinger4, SimulationSettings.Instance.VisualizeBearing4);
    }

    private void SetBearingActive(GameObject trueBearing, GameObject expectedBearing, bool active)
    {
        if (trueBearing.activeSelf != active) trueBearing.SetActive(active);
        if (expectedBearing.activeSelf != active) expectedBearing.SetActive(active);
    }

    private void thetaXCallback(Float64Msg thetaX)
    {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.z = (float)-thetaX.data;
    }
    private void thetaYCallback(Float64Msg thetaY)
    {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.x = (float)thetaY.data;
    }
    private void thetaZCallback(Float64Msg thetaZ)
    {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVRot.y = (float)-thetaZ.data;
    }
    private void posXCallback(Float64Msg X)
    {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVPos.z = (float)X.data;
    }
    private void posYCallback(Float64Msg Y)
    {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVPos.x = (float)-Y.data;
    }
    private void posZCallback(Float64Msg Z)
    {
        if (!dianaVisualization.activeSelf) dianaVisualization.SetActive(true);
        currentAUVPos.y = (float)Z.data;
    }

    private void objectMapCallback(VisionObjectArrayMsg map)
    {
        int num_lane_markers_in_map = 0;
        int num_abydos_symbols_in_map = 0;
        int num_earth_symbols_in_map = 0;
        bool buoyInMap = false;
        bool gateInMap = false;
        bool octagonTableInMap = false;
        bool binInMap = false;
        foreach (VisionObjectMsg detection in map.array)
        {
            if (detection.label == "Lane Marker")
            {
                GameObject fromVis = num_lane_markers_in_map == 0 ? laneMarker1FromVisualization : laneMarker2FromVisualization;
                GameObject toVis = num_lane_markers_in_map == 0 ? laneMarker1ToVisualization : laneMarker2ToVisualization;
                
                UpdateVisualizationTransform(fromVis, detection, false);
                UpdateVisualizationTransform(toVis, detection, false);
                
                fromVis.transform.rotation = Quaternion.Euler(-90, (float)-detection.theta_z, 0) * Quaternion.Euler(0, 0, 90);
                toVis.transform.rotation = Quaternion.Euler(-90, (float)-detection.extra_field, 0) * Quaternion.Euler(0, 0, 90);

                num_lane_markers_in_map += 1;
            }
            else if (detection.label == "Gate")
            {
                gateInMap = true;
                UpdateVisualizationTransform(gateVisualization, detection, true);
                if (detection.extra_field > 0.5) gateVisualization.transform.localScale = new Vector3(-1, 1, 1);
            }
            else if (detection.label == "Buoy")
            {
                buoyInMap = true;
                UpdateVisualizationTransform(buoyVisualization, detection, false);
                buoyVisualization.transform.rotation = Quaternion.Euler(0, (float)-detection.theta_z + 90, 0);
            }
            else if (detection.label == "Bin")
            {
                binInMap = true;
                UpdateVisualizationTransform(binVisualization, detection, true);
            }
            else if (detection.label == "Octagon Table")
            {
                octagonTableInMap = true;
                UpdateVisualizationTransform(octagonTableVisualization, detection, false);
            }
            else if (detection.label == "Abydos Symbol")
            {
                GameObject vis = num_abydos_symbols_in_map == 0 ? abydosSymbol1Visualization : abydosSymbol2Visualization;
                UpdateVisualizationTransform(vis, detection, false);
                vis.transform.rotation = Quaternion.Euler(0, num_abydos_symbols_in_map == 0 ? (float)-detection.theta_z : (float)-detection.extra_field, 0);
                num_abydos_symbols_in_map += 1;
            }
            else if (detection.label == "Earth Symbol")
            {
                GameObject vis = num_earth_symbols_in_map == 0 ? earthSymbol1Visualization : earthSymbol2Visualization;
                UpdateVisualizationTransform(vis, detection, false);
                vis.transform.rotation = Quaternion.Euler(0, num_earth_symbols_in_map == 0 ? (float)-detection.theta_z : (float)-detection.extra_field, 0);
                num_earth_symbols_in_map += 1;
            }
        }
        
        SetVisualizationActive(laneMarker1FromVisualization, num_lane_markers_in_map > 0);
        SetVisualizationActive(laneMarker1ToVisualization, num_lane_markers_in_map > 0);
        SetVisualizationActive(laneMarker2FromVisualization, num_lane_markers_in_map > 1);
        SetVisualizationActive(laneMarker2ToVisualization, num_lane_markers_in_map > 1);
        SetVisualizationActive(buoyVisualization, buoyInMap);
        SetVisualizationActive(gateVisualization, gateInMap);
        SetVisualizationActive(binVisualization, binInMap);
        SetVisualizationActive(octagonTableVisualization, octagonTableInMap);
        SetVisualizationActive(abydosSymbol1Visualization, num_abydos_symbols_in_map > 0);
        SetVisualizationActive(abydosSymbol2Visualization, num_abydos_symbols_in_map > 1);
        SetVisualizationActive(earthSymbol1Visualization, num_earth_symbols_in_map > 0);
        SetVisualizationActive(earthSymbol2Visualization, num_earth_symbols_in_map > 1);
    }

    private void UpdateVisualizationTransform(GameObject obj, VisionObjectMsg detection, bool updateRotation)
    {
        obj.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
        if (updateRotation)
        {
            obj.transform.rotation = Quaternion.Euler(0, (float)-detection.theta_z, 0);
        }
    }

    private void SetVisualizationActive(GameObject obj, bool active)
    {
        if (obj.activeSelf != active) obj.SetActive(active);
    }

    private void detectionFrameCallback(VisionObjectArrayMsg detectionFrame)
    {
        foreach (VisionObjectMsg detection in detectionFrame.array)
        {
            GameObject detectionObject;
            if (detectionFrameIndicators.Count >= maxDetectionFrameIndicators)
            {
                detectionObject = detectionFrameIndicators[currentIndex % maxDetectionFrameIndicators];
                currentIndex++;
            }
            else
            {
                detectionObject = GameObject.Instantiate(detectionIndicatorPrefab);
                detectionFrameIndicators.Add(detectionObject);
            }
            detectionObject.transform.position = new Vector3((float)-detection.y, (float)detection.z, (float)detection.x);
            detectionObject.transform.SetParent(transform);
        }
    }
}