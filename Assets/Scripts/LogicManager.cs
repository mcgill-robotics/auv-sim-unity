using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using TMPro;

public class LogicManager1 : MonoBehaviour
{
    public GameObject downCam;
    public GameObject frontCam;
    public GameObject freeCam;
    public GameObject followCam;
    public Transform auv;
    public float distanceToAUVWhenSnapping;

    public string xSetpointTopicName;
    public string ySetpointTopicName;
    public string zSetpointTopicName;
    public string quatSetpointTopicName;
    
    public TMPro.TMP_InputField xInputField;
    public TMPro.TMP_InputField yInputField;
    public TMPro.TMP_InputField zInputField;
    public TMPro.TMP_InputField rotXInputField;
    public TMPro.TMP_InputField rotYInputField;
    public TMPro.TMP_InputField rotZInputField;

    public TMP_Text XPosText;
    public TMP_Text YPosText;
    public TMP_Text ZPosText;
    public TMP_Text RotXText;
    public TMP_Text RotYText;
    public TMP_Text RotZText;

    private ROSConnection roscon;

    // Start is called before the first frame update
    void Start()
    {
        roscon = ROSConnection.GetOrCreateInstance();
        roscon.RegisterPublisher<Float64Msg>(xSetpointTopicName); 
        roscon.RegisterPublisher<Float64Msg>(ySetpointTopicName); 
        roscon.RegisterPublisher<Float64Msg>(zSetpointTopicName); 
        roscon.RegisterPublisher<RosMessageTypes.Geometry.QuaternionMsg>(quatSetpointTopicName); 
        activateFollowCam();
        activateFreeCam();
    }

    void Update()
    {
        XPosText.text = "Current X: " + auv.position.z;
        YPosText.text = "Current Y: " + -auv.position.x;
        ZPosText.text = "Current Z: " + auv.position.y;

        Quaternion rollQuaternion = Quaternion.Euler(0f, 0f, auv.eulerAngles.z);
        Quaternion pitchQuaternion = Quaternion.Euler(auv.eulerAngles.x, 0f, 0f);
        Quaternion yawQuaternion = Quaternion.Euler(0f, auv.eulerAngles.y + 90f, 0f);
        Quaternion rotation = rollQuaternion * pitchQuaternion * yawQuaternion; // to specify XYZ order of euler angles
        RotXText.text = "Current Euler X: " + -rotation.eulerAngles.x;
        RotYText.text = "Current Euler Y: " + -rotation.eulerAngles.z;
        RotZText.text = "Current Euler Z: " + rotation.eulerAngles.y;
    }

    public void activateDownCam() {
        downCam.SetActive(true);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(false);
    }

    public void activateFrontCam() {
        downCam.SetActive(false);
        frontCam.SetActive(true);
        freeCam.SetActive(false);
        followCam.SetActive(false);
    }

    public void activateFreeCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(true);
        followCam.SetActive(false);
    }
    
    public void activateFollowCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(true);
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

        msg.w = setpoint.w;
        msg.x = setpoint.x;
        msg.y = setpoint.y;
        msg.z = setpoint.z;
        roscon.Publish(quatSetpointTopicName, msg);
    }
}
