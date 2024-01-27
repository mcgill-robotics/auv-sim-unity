using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerPreferenceLoader : MonoBehaviour
{
    public LogicManager1 logicManagerScript;

    public Toggle PublishDownCamToggle;
    public Toggle PublishFrontCamToggle;
    public Toggle PublishDepthToggle;
    public Toggle PublishHydrophonesToggle;
    public Toggle PublishDVLToggle;
    public Toggle PublishIMUToggle;
    public Toggle PublishROSToggle;
    public Toggle DisplaySimToggle;

    public Toggle PIDGuiToggle;
    public Toggle CameraGuiToggle;
    public Toggle DebugInfoGuiToggle;
    public Toggle SensorSelectGuiToggle;
    public Toggle VisualizeBeliefsToggle;

    public GameObject settingsGUI;
    //ROS SETTINGS UI ELEMENTS
    public TMPro.TMP_InputField frontCamRateInputField;
    public TMPro.TMP_InputField downCamRateInputField;
    public TMPro.TMP_InputField frontCamWidthInputField;
    public TMPro.TMP_InputField frontCamHeightInputField;
    public TMPro.TMP_InputField downCamWidthInputField;
    public TMPro.TMP_InputField downCamHeightInputField;
    public TMPro.TMP_InputField poseRateInputField;
    // QUALITY SETTING DROPDOWN
    public TMP_Dropdown qualityDropdown;
    //KEYBIND UI ELEMENTS
    // POSITION KEYBINDS
    public TMP_Text surgeKeybindText;
    public TMP_Text negSurgeKeybindText;
    public TMP_Text swayKeybindText;
    public TMP_Text negSwayKeybindText;
    public TMP_Text heaveKeybindText;
    public TMP_Text negHeaveKeybindText;
    // ROTATION KEYBINDS
    public TMP_Text yawKeybindText;
    public TMP_Text negYawKeybindText;
    public TMP_Text pitchKeybindText;
    public TMP_Text negPitchKeybindText;
    public TMP_Text rollKeybindText;
    public TMP_Text negRollKeybindText;
    // FREEZE KEYBIND
    public TMP_Text freezeKeybindText;

    // Start is called before the first frame update
    void Start()
    {
        // update toggles to be at correct values based on player preferences
        // done in specific order so that toggles which deactivate each other happen last
        PublishDownCamToggle.isOn = bool.Parse(load("PublishDownCamToggle", "true"));
        PublishFrontCamToggle.isOn = bool.Parse(load("PublishFrontCamToggle", "true"));
        PublishDepthToggle.isOn = bool.Parse(load("PublishDepthToggle", "true"));
        PublishHydrophonesToggle.isOn = bool.Parse(load("PublishHydrophonesToggle", "true"));
        PublishDVLToggle.isOn = bool.Parse(load("PublishDVLToggle", "true"));
        PublishIMUToggle.isOn = bool.Parse(load("PublishIMUToggle", "true"));
        PublishROSToggle.isOn = bool.Parse(load("PublishROSToggle", "true"));
        DisplaySimToggle.isOn = bool.Parse(load("DisplaySimToggle", "true"));

        PIDGuiToggle.isOn = bool.Parse(load("PIDGuiToggle", "true"));
        CameraGuiToggle.isOn = bool.Parse(load("CameraGuiToggle", "true"));
        DebugInfoGuiToggle.isOn = bool.Parse(load("DebugInfoGuiToggle", "true"));
        SensorSelectGuiToggle.isOn = bool.Parse(load("SensorSelectGuiToggle", "true"));
        VisualizeBeliefsToggle.isOn = bool.Parse(load("VisualizeBeliefsToggle", "true"));

        // activate settings to ensure settings UI will update instantly
        settingsGUI.SetActive(true);

        // ROS INPUT FIELD VALUES
        frontCamRateInputField.text = load("frontCamRate", "10");
        downCamRateInputField.text = load("downCamRate", "10");
        frontCamWidthInputField.text = load("frontCamWidth", "640");
        frontCamHeightInputField.text = load("frontCamHeight", "480");
        downCamWidthInputField.text = load("downCamWidth", "640");
        downCamHeightInputField.text = load("downCamHeight", "480");
        poseRateInputField.text = load("poseRate", "10");

        // QUALITY SETTING
        qualityDropdown.value = int.Parse(load("qualityLevel", "3"));

        // KEYBIND Text LABELS
        surgeKeybindText.text = load("surgeKeybind", "w");
        negSurgeKeybindText.text = load("negSurgeKeybind", "s");
        swayKeybindText.text = load("swayKeybind", "a");
        negSwayKeybindText.text = load("negSwayKeybind", "d");
        heaveKeybindText.text = load("heaveKeybind", "e");
        negHeaveKeybindText.text = load("negHeaveKeybind", "q");
        yawKeybindText.text = load("yawKeybind", "j");
        negYawKeybindText.text = load("negYawKeybind", "l");
        pitchKeybindText.text = load("pitchKeybind", "i");
        negPitchKeybindText.text = load("negPitchKeybind", "k");
        rollKeybindText.text = load("rollKeybind", "o");
        negRollKeybindText.text = load("negRollKeybind", "u");
        freezeKeybindText.text = load("freezeKeybind", "space");
        
        settingsGUI.SetActive(false);

        // CALL LOGIC MANAGER SCRIPT FUNCTIONS TO APPLY LOADING
        logicManagerScript.setQualityLevel();

    }

    string load(string key, string defaultValue)
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }
}
