using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class VisualizeAUVBeliefs : MonoBehaviour
{
    [Header("AUV Visualization")]
    [Tooltip("GameObject representing the AUV's estimated pose from ROS state")]
    public GameObject dianaVisualization;
    
    [Space(10)]
    [Header("Lane Marker Visualizations")]
    [Tooltip("Lane marker 1 start position visualization")]
    public GameObject laneMarker1FromVisualization;
    
    [Tooltip("Lane marker 1 end position visualization")]
    public GameObject laneMarker1ToVisualization;
    
    [Tooltip("Lane marker 2 start position visualization")]
    public GameObject laneMarker2FromVisualization;
    
    [Tooltip("Lane marker 2 end position visualization")]
    public GameObject laneMarker2ToVisualization;
    
    [Space(10)]
    [Header("Task Object Visualizations")]
    [Tooltip("Gate object visualization from object map")]
    public GameObject gateVisualization;
    
    [Tooltip("Buoy object visualization from object map")]
    public GameObject buoyVisualization;
    
    [Tooltip("Octagon table visualization from object map")]
    public GameObject octagonTableVisualization;
    
    [Tooltip("Bin object visualization from object map")]
    public GameObject binVisualization;
    
    [Space(5)]
    [Tooltip("Abydos symbol 1 visualization from object map")]
    public GameObject abydosSymbol1Visualization;
    
    [Tooltip("Abydos symbol 2 visualization from object map")]
    public GameObject abydosSymbol2Visualization;
    
    [Tooltip("Earth symbol 1 visualization from object map")]
    public GameObject earthSymbol1Visualization;
    
    [Tooltip("Earth symbol 2 visualization from object map")]
    public GameObject earthSymbol2Visualization;
    
    [Space(10)]
    [Header("Detection Indicators")]
    [Tooltip("Prefab for instantiating detection frame indicators")]
    public GameObject detectionIndicatorPrefab;
    
    [Tooltip("Maximum number of detection frame indicators before reusing")]
    [Range(1, 50)]
    public int maxDetectionFrameIndicators = 10;
    
    [Space(10)]
    [Header("Pinger Bearing Visualizations")]
    [Tooltip("True bearing line for pinger 1")]
    public GameObject trueBearingPinger1;
    
    [Tooltip("True bearing line for pinger 2")]
    public GameObject trueBearingPinger2;
    
    [Tooltip("True bearing line for pinger 3")]
    public GameObject trueBearingPinger3;
    
    [Tooltip("True bearing line for pinger 4")]
    public GameObject trueBearingPinger4;
    
    [Space(5)]
    [Tooltip("Expected bearing line for pinger 1 from ROS estimate")]
    public GameObject expectedBearingPinger1;
    
    [Tooltip("Expected bearing line for pinger 2 from ROS estimate")]
    public GameObject expectedBearingPinger2;
    
    [Tooltip("Expected bearing line for pinger 3 from ROS estimate")]
    public GameObject expectedBearingPinger3;
    
    [Tooltip("Expected bearing line for pinger 4 from ROS estimate")]
    public GameObject expectedBearingPinger4;
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