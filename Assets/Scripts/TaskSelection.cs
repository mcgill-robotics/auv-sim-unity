using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class TaskSelection : MonoBehaviour
{
	public GameObject Diana;
	public GameObject PoleQuali;
	public GameObject GateQuali;
	public GameObject Gate;
	public GameObject LaneMarkerStraight;
	public GameObject LaneMarker45Left;
	public GameObject Buoy;
	public GameObject Pinger1;
	public GameObject Pinger2;
	public GameObject Pinger3;
	public GameObject Pinger4;
	public GameObject OctagonTable;
	public GameObject Bin;
	Dictionary<string, GameObject> objectsDictionary = new Dictionary<string, GameObject>();
	public TMP_Dropdown TaskSelectionDropdown;
	List<Dictionary<string, Vector3>> objectsPosition = new List<Dictionary<string, Vector3>>() {
		// Competition 
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(-13, -0.2f, -14)},
			{"Gate", new Vector3(-9, -1, -12)},
			{"LaneMarkerStraight", new Vector3(-6.5f, -5, -11.5f)},
			{"Buoy", new Vector3(4, 0, -13)},
			{"LaneMarker45Left", new Vector3(1.5f, -5, -9)},
			{"Bin", new Vector3(-4, -5, 4)},
			{"Pinger1", new Vector3(9, -5, -5)},
			// Missing Torpedos prop
			{"Pinger2", new Vector3(7, -5, 10)},
			{"OctagonTable", new Vector3(7, -5, 10)}
		},
		// Quali
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, -3)},
			{"GateQuali", new Vector3(0, -1, 0)},
			{"PoleQuali", new Vector3(0, -3, 10)}
		},
		// Gate
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)},
			{"Gate", new Vector3(2, -1, 2)}
		},
		// Buoy
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)},
			{"Buoy", new Vector3(0, 0, 3)}
		},
		// Bins
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)},
			{"Bin", new Vector3(0, -5, 3)}
		}, 
		// Torpedo
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)}
		},
		// Octagon
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)},
			{"OctagonTable", new Vector3(0, -5, 3)}
		},
		// Lane Marker
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)},
			{"LaneMarker45Left", new Vector3(2, -5, 2)},
			{"LaneMarkerStraight", new Vector3(-5, -5, 7)},
		},
		// Pinger
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -0.2f, 0)},
			{"Pinger1", new Vector3(10, -3, 10)},
			{"Pinger2", new Vector3(10, -3, -10)},
			{"Pinger3", new Vector3(-10, -3, -10)},
			{"Pinger4", new Vector3(-10, -3, 10)}
		}
	};

	List<Dictionary<string, Vector3>> objectsEulerAngles = new List<Dictionary<string, Vector3>>() {
		// Competition 
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, 0, 0)},
			{"Gate", new Vector3(0, 75, 0)},
			{"LaneMarkerStraight", new Vector3(0, 75, 0)},
			{"Buoy", new Vector3(4, 0, -13)},
			{"LaneMarker45Left", new Vector3(0, 30, 0)},
			{"Bin", new Vector3(0, 45, 0)},
			{"Pinger1", new Vector3(0, 0, 0)},
			// Missing Torpedos prop
			{"Pinger2", new Vector3(0, 0, 0)},
			{"OctagonTable", new Vector3(0, 0, 0)}
		},
		// Quali
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"GateQuali", new Vector3(0, 0, 0)},
			{"PoleQuali", new Vector3(0, 0, 0)}
		},
		// Gate
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"Gate", new Vector3(0, 45, 0)}
		},
		// Buoy
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"Buoy", new Vector3(0, 0, 0)}
		},
		// Bins
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"Bin", new Vector3(0, 0, 0)}
		},
		// Torpedo
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)}
		},
		// Octagon
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"OctagonTable", new Vector3(0, 0, 0)}
		},
		// Lane Marker
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"LaneMarker45Left", new Vector3(0, 0, 0)},
			{"LaneMarkerStraight", new Vector3(0, 75, 0)}
		},
		// Pinger
		new Dictionary<string, Vector3> {
			{"AUV", new Vector3(0, -90, 0)},
			{"Pinger1", new Vector3(0, 0, 0)},
			{"Pinger2", new Vector3(0, 0, 0)},
			{"Pinger3", new Vector3(0, 0, 0)},
			{"Pinger4", new Vector3(0, 0, 0)}
		}
	};
	List<string> tasksNameList;
	public int current_option = 0;

	void Start()
	{
		objectsDictionary.Add("AUV", Diana);
		objectsDictionary.Add("PoleQuali", PoleQuali);
		objectsDictionary.Add("GateQuali", GateQuali);
		objectsDictionary.Add("Gate", Gate);
		objectsDictionary.Add("LaneMarkerStraight", LaneMarkerStraight);
		objectsDictionary.Add("LaneMarker45Left", LaneMarker45Left);
		objectsDictionary.Add("Buoy", Buoy);
		objectsDictionary.Add("Pinger1", Pinger1);
		objectsDictionary.Add("Pinger2", Pinger2);
		objectsDictionary.Add("Pinger3", Pinger3);
		objectsDictionary.Add("Pinger4", Pinger4);
		objectsDictionary.Add("OctagonTable", OctagonTable);
		objectsDictionary.Add("Bin", Bin);

		tasksNameList = new List<string>(objectsDictionary.Keys);
		SetTaskEnvironment();

		TaskSelectionDropdown.onValueChanged.AddListener(delegate
		{
			DropdownValueChanged(TaskSelectionDropdown);
		});
	}

	void DropdownValueChanged(TMP_Dropdown change)
	{
		current_option = change.value;
		SetTaskEnvironment();
	}

	public void SetTaskEnvironment()
	{
		foreach (string tasksName in tasksNameList)
		{
			objectsDictionary[tasksName].SetActive(false);
			if (objectsPosition[current_option].ContainsKey(tasksName) && objectsEulerAngles[current_option].ContainsKey(tasksName))
			{
				objectsDictionary[tasksName].transform.position = objectsPosition[current_option][tasksName];
				objectsDictionary[tasksName].transform.eulerAngles = objectsEulerAngles[current_option][tasksName];
			}
			else
			{
				objectsDictionary[tasksName].transform.position = new Vector3(0, -100, 0);
				objectsDictionary[tasksName].transform.eulerAngles = new Vector3(0, -100, 0);
			}
			objectsDictionary[tasksName].SetActive(true);
		}
	}
}