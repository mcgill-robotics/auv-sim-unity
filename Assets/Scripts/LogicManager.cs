using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using TMPro;

public class LogicManager1 : MonoBehaviour
{
    [Header("CAMERA CONTROL VARIABLES")]

    public GameObject downCam;
    public GameObject frontCam;
    public GameObject freeCam;
    public GameObject followCam;
    public GameObject depthCam;
    public Transform auv;
    public float distanceToAUVWhenSnapping;

    [Header("PID GUI VARIABLES")]

    public string xSetpointTopicName;
    public string ySetpointTopicName;
    public string zSetpointTopicName;
    public string quatSetpointTopicName;
    
    public string xPositionTopicName;
    public string yPositionTopicName;
    public string zPositionTopicName;
    public string thetaXTopicName;
    public string thetaYTopicName;
    public string thetaZTopicName;

    public TMPro.TMP_InputField xInputField;
    public TMPro.TMP_InputField yInputField;
    public TMPro.TMP_InputField zInputField;
    public TMPro.TMP_InputField rotXInputField;
    public TMPro.TMP_InputField rotYInputField;
    public TMPro.TMP_InputField rotZInputField;

    [Header("DEBUG INFO VARIABLES")]

    public TMP_Text XPosText;
    public TMP_Text YPosText;
    public TMP_Text ZPosText;
    public TMP_Text RotXText;
    public TMP_Text RotYText;
    public TMP_Text RotZText;

    [Header("FOR QUALITY SETTINGS")]
    public TMP_Dropdown qualityDropdown;

    [Header("FOR PUBLISHER TOGGLE SETTINGS")]
    public Toggle PublishROSToggle;


    private ROSConnection roscon;

    // Start is called before the first frame update
    void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.Subscribe<Float64Msg>(xPositionTopicName, xPositionCallback);
        roscon.Subscribe<Float64Msg>(yPositionTopicName, yPositionCallback);
        roscon.Subscribe<Float64Msg>(zPositionTopicName, zPositionCallback);
        roscon.Subscribe<Float64Msg>(thetaXTopicName, thetaXCallback);
        roscon.Subscribe<Float64Msg>(thetaYTopicName, thetaYCallback);
        roscon.Subscribe<Float64Msg>(thetaZTopicName, thetaZCallback);
        roscon.RegisterPublisher<Float64Msg>(xSetpointTopicName); 
        roscon.RegisterPublisher<Float64Msg>(ySetpointTopicName); 
        roscon.RegisterPublisher<Float64Msg>(zSetpointTopicName); 
        roscon.RegisterPublisher<RosMessageTypes.Geometry.QuaternionMsg>(quatSetpointTopicName); 
        activateFollowCam();
        activateFreeCam();
    }

    void xPositionCallback(Float64Msg msg) {
        XPosText.text = "Current X: " + msg.data;
    }
    
    void yPositionCallback(Float64Msg msg) {
        YPosText.text = "Current Y: " + msg.data;
    }
    
    void zPositionCallback(Float64Msg msg) {
        ZPosText.text = "Current Z: " + msg.data;
    }
    
    void thetaXCallback(Float64Msg msg) {
        RotXText.text = "Current Euler X: " + msg.data;
    }
    
    void thetaYCallback(Float64Msg msg) {
        RotYText.text = "Current Euler Y: " + msg.data;
    }
    
    void thetaZCallback(Float64Msg msg) {
        RotZText.text = "Current Euler Z: " + msg.data;
    }

    public void activateDownCam() {
        downCam.SetActive(true);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(false);
        depthCam.SetActive(false);
    }

    public void activateFrontCam() {
        downCam.SetActive(false);
        frontCam.SetActive(true);
        freeCam.SetActive(false);
        followCam.SetActive(false);
        depthCam.SetActive(false);
    }

    public void activateFreeCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(true);
        followCam.SetActive(false);
        depthCam.SetActive(false);
    }
    
    public void activateFollowCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(true);
        depthCam.SetActive(false);
    }

    public void activateDepthCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(false);
        depthCam.SetActive(true);
    }

    public void snapFreeCam() {
        if (freeCam.activeSelf) {
            freeCam.transform.LookAt(auv);
            Vector3 directionFromTarget = freeCam.transform.position - auv.position;
            freeCam.transform.position = auv.position + directionFromTarget.normalized * distanceToAUVWhenSnapping;
        }
    }
    
    public void reloadScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void setXPID()
    {
        Float64Msg msg = new Float64Msg();
        msg.data = float.Parse(xInputField.text);
        roscon.Publish(xSetpointTopicName, msg);
    }

    public void setYPID()
    {
        Float64Msg msg = new Float64Msg();
        msg.data = float.Parse(yInputField.text);
        roscon.Publish(ySetpointTopicName, msg);
    }
    
    public void setZPID()
    {
        Float64Msg msg = new Float64Msg();
        msg.data = float.Parse(zInputField.text);
        roscon.Publish(zSetpointTopicName, msg);
    }
    
    public void setQuatPID()
    {
        RosMessageTypes.Geometry.QuaternionMsg msg = new RosMessageTypes.Geometry.QuaternionMsg();
        Quaternion rollQuaternion = Quaternion.Euler(0f, 0f, float.Parse(rotXInputField.text));
        Quaternion pitchQuaternion = Quaternion.Euler(float.Parse(rotYInputField.text), 0f, 0f);
        Quaternion yawQuaternion = Quaternion.Euler(0f, float.Parse(rotZInputField.text), 0f);
        Quaternion setpoint = rollQuaternion * pitchQuaternion * yawQuaternion; // to specify XYZ order of euler angles

        msg = setpoint.To<NED>();
        roscon.Publish(quatSetpointTopicName, msg);
    }

    public void setQualityLevel() {
        QualitySettings.SetQualityLevel(qualityDropdown.value, true);
        PlayerPrefs.SetString("qualityLevel", qualityDropdown.value.ToString());
        PlayerPrefs.Save();
    }

    public void setROSPublishToggle() {
        PlayerPrefs.SetString("PublishROSToggle", PublishROSToggle.isOn.ToString());
        PlayerPrefs.Save();
    }

}
