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
    public string pidQuatEnableName = "/controls/pid/quat/enable";
    public string pidXEnableName = "/controls/pid/x/enable";
    public string pidYEnableName = "/controls/pid/y/enable";
    public string pidZEnableName = "/controls/pid/z/enable";


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
    public GameObject waterObject;

    [Header("FOR PUBLISHER TOGGLE SETTINGS")]
    public Toggle PublishDVLToggle;
    public Toggle PublishROSToggle;
    public Toggle DisplaySimToggle;
    public Toggle PublishIMUToggle;
    public LayerMask hiddenSimLayerMask;
    private LayerMask followCamDefaultLayerMask; 
    private LayerMask freeCamDefaultLayerMask; 
    public Toggle PublishDepthToggle;
    public Toggle PublishHydrophonesToggle;
    public Toggle PublishFrontCamToggle;
    public Toggle PublishDownCamToggle;

    private ROSConnection roscon; 

    public TMPro.TMP_InputField frontCamRateInputField;
    public TMPro.TMP_InputField downCamRateInputField;
    public TMPro.TMP_InputField frontCamWidthInputField;
    public TMPro.TMP_InputField frontCamHeightInputField;
    public TMPro.TMP_InputField downCamWidthInputField;
    public TMPro.TMP_InputField downCamHeightInputField;
    public TMPro.TMP_InputField poseRateInputField;

    private static bool isCheckingKey = false;
    private Color originalButtonColor;
    private int timeoutInSeconds = 7;
    public CameraPublisher frontCamPub;
    public CameraPublisher downCamPub;
    public CameraDepthPublisher depthCamPub;


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
        roscon.RegisterPublisher<BoolMsg>(pidQuatEnableName);
        roscon.RegisterPublisher<BoolMsg>(pidXEnableName);
        roscon.RegisterPublisher<BoolMsg>(pidYEnableName);
        roscon.RegisterPublisher<BoolMsg>(pidZEnableName);
        roscon.RegisterPublisher<RosMessageTypes.Geometry.QuaternionMsg>(quatSetpointTopicName); 
        activateFollowCam();
        activateFreeCam();
        followCamDefaultLayerMask = followCam.GetComponent<Camera>().cullingMask;
        freeCamDefaultLayerMask = freeCam.GetComponent<Camera>().cullingMask;
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
        BoolMsg bool_msg = new BoolMsg(false);
        roscon.Publish(pidXEnableName, bool_msg);
        roscon.Publish(pidYEnableName, bool_msg);
        roscon.Publish(pidZEnableName, bool_msg);
        
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

    }

    public void setXPID()
    {   
        BoolMsg bool_msg = new BoolMsg(true);
        roscon.Publish(pidXEnableName, bool_msg);

        Float64Msg msg = new Float64Msg();
        msg.data = float.Parse(xInputField.text);
        roscon.Publish(xSetpointTopicName, msg);
    }

    public void setYPID()
    {
        BoolMsg bool_msg = new BoolMsg(true);
        roscon.Publish(pidYEnableName, bool_msg);

        Float64Msg msg = new Float64Msg();
        msg.data = float.Parse(yInputField.text);
        roscon.Publish(ySetpointTopicName, msg);
    }
    
    public void setZPID()
    {   
        BoolMsg bool_msg = new BoolMsg(true);
        roscon.Publish(pidZEnableName, bool_msg);

        Float64Msg msg = new Float64Msg();
        msg.data = float.Parse(zInputField.text);
        roscon.Publish(zSetpointTopicName, msg);
    }
    
    public void setQuatPID()
    {   
        BoolMsg bool_msg = new BoolMsg(true);
        roscon.Publish(pidQuatEnableName, bool_msg);

        RosMessageTypes.Geometry.QuaternionMsg msg = new RosMessageTypes.Geometry.QuaternionMsg();
        Quaternion rollQuaternion = Quaternion.Euler(0f, 0f, -float.Parse(rotXInputField.text));
        Quaternion pitchQuaternion = Quaternion.Euler(-float.Parse(rotYInputField.text), 0f, 0f);
        Quaternion yawQuaternion = Quaternion.Euler(0f, float.Parse(rotZInputField.text), 0f);
        Quaternion setpoint = rollQuaternion * pitchQuaternion * yawQuaternion; // to specify XYZ order of euler angles

        msg = setpoint.To<NED>();
        roscon.Publish(quatSetpointTopicName, msg);
    }

    public void setQualityLevel() {
        QualitySettings.SetQualityLevel(qualityDropdown.value, true);
        PlayerPrefs.SetString("qualityLevel", qualityDropdown.value.ToString());
        PlayerPrefs.Save();
        if (qualityDropdown.value == 3) { //turn off water on barebones
            waterObject.SetActive(false);
        } else {
            waterObject.SetActive(true);
        }
    }

    public void setROSPublishToggle() {
        PlayerPrefs.SetString("PublishROSToggle", PublishROSToggle.isOn.ToString());
        PlayerPrefs.Save();
    }

    public void hideSimObjects() {
        if (DisplaySimToggle.isOn) {
            freeCam.GetComponent<Camera>().cullingMask = freeCamDefaultLayerMask;
            followCam.GetComponent<Camera>().cullingMask = followCamDefaultLayerMask;
        } else {
            freeCam.GetComponent<Camera>().cullingMask = hiddenSimLayerMask;
            followCam.GetComponent<Camera>().cullingMask = hiddenSimLayerMask;
        }
        PlayerPrefs.SetString("DisplaySimToggle", DisplaySimToggle.isOn.ToString());
        PlayerPrefs.Save();
    }


    public void setPublishDVLToggle(){
         PlayerPrefs.SetString("PublishDVLToggle", PublishDVLToggle.isOn.ToString());
         PlayerPrefs.Save();
    }

    public void setPublishIMUToggle(){
         PlayerPrefs.SetString("PublishIMUToggle", PublishIMUToggle.isOn.ToString());
         PlayerPrefs.Save();
    }

   public void setPublishDepthToggle(){
         PlayerPrefs.SetString("PublishDepthToggle", PublishDepthToggle.isOn.ToString());
         PlayerPrefs.Save();
    }

    public void setPublishHydrophonesToggle(){
         PlayerPrefs.SetString("PublishHydrophonesToggle", PublishHydrophonesToggle.isOn.ToString());
         PlayerPrefs.Save();
    }
    public void setPublishFrontCamToggle(){
         PlayerPrefs.SetString("PublishFrontCamToggle", PublishFrontCamToggle.isOn.ToString());
         PlayerPrefs.Save();
    }

    public void setPublishDownCamToggle(){
         PlayerPrefs.SetString("PublishDownCamToggle", PublishDownCamToggle.isOn.ToString());
         PlayerPrefs.Save();
    }

    public void OnButtonClick(Button clickedButton)
    {
        if (!isCheckingKey){
            Image buttonImage = clickedButton.GetComponent<Image>();
            
            if (buttonImage != null){
                originalButtonColor = buttonImage.color;
            }
            
            ChangeButtonColor(clickedButton, new Color(1f, 0f, 0f, 0.5f));
            StartCoroutine(CheckForKey(clickedButton));

        } else {
            return;
        }
    }

    public void OnClickRates(Button clickedButton){

        if (clickedButton.name == "SetFrontCamRateBtn"){
            PlayerPrefs.SetString("frontCamRate", frontCamRateInputField.text);
            frontCamPub.Initialize();
            depthCamPub.Initialize();
        }
        if (clickedButton.name == "SetDownCamRateBtn"){
            PlayerPrefs.SetString("downCamRate", downCamRateInputField.text);
            downCamPub.Initialize();
        }
        if (clickedButton.name == "SetFrontCamResBtn"){
            PlayerPrefs.SetString("frontCamWidth", frontCamWidthInputField.text);
            PlayerPrefs.SetString("frontCamHeight", frontCamHeightInputField.text);
            frontCamPub.Initialize();
            depthCamPub.Initialize();
        }
        if (clickedButton.name == "SetDownCamResBtn"){
            PlayerPrefs.SetString("downCamHeight", downCamHeightInputField.text);
            PlayerPrefs.SetString("downCamWidth", downCamWidthInputField.text);
            downCamPub.Initialize();
        }
        if (clickedButton.name == "SetPoseRateBtn"){
            PlayerPrefs.SetString("poseRate", poseRateInputField.text);
        }
        PlayerPrefs.Save();

    }

    private System.Collections.IEnumerator CheckForKey(Button clickedButton)
    {
        isCheckingKey = true;

        float startTime = Time.time;

        while (Time.time - startTime < timeoutInSeconds)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Escape)
                    {
                    isCheckingKey = false;
                    ChangeButtonColor(clickedButton, originalButtonColor);
                    yield break;
                    }
                    
                    if ((keyCode >= KeyCode.A && keyCode <= KeyCode.Z) || (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9) || (keyCode == KeyCode.Space))
                    {
                        TextMeshProUGUI buttonText = clickedButton.GetComponentInChildren<TextMeshProUGUI >();
                        string newText = keyCode.ToString().ToLower();
                        buttonText.text = newText;
                        isCheckingKey = false;
                        setPlayerRefsKeys(clickedButton, newText);
                        ChangeButtonColor(clickedButton, originalButtonColor);
                        yield break;
                    }
                    else
                    {
                        yield return null;
                    }
                    
                }
            }
            yield return null;
        }
        isCheckingKey = false;
        ChangeButtonColor(clickedButton, originalButtonColor);
    }

    private void ChangeButtonColor(Button button, Color newColor)
    {
        if (button != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = newColor;
            }
            else
            {
                Debug.LogError("Image component not found on the button GameObject.");
            }
        }
        else
        {
            Debug.LogError("Button component not assigned.");
        }
    }

    private void setPlayerRefsKeys(Button button, string keyCode){
        // movement keybinds
        if (button.name == "ForwardKeybind"){
            PlayerPrefs.SetString("surgeKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "BackwardKeybind"){
            PlayerPrefs.SetString("negSurgeKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "LeftKeybind"){
            PlayerPrefs.SetString("swayKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "RightKeybind"){
            PlayerPrefs.SetString("negSwayKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "HeaveKeybind"){
            PlayerPrefs.SetString("heaveKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "NegativeHeaveKeybind"){
            PlayerPrefs.SetString("negHeaveKeybind", keyCode);
            PlayerPrefs.Save();
        }

        // rotations keybinds
        if (button.name == "YawKeybind"){
            PlayerPrefs.SetString("yawKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "NegativeYawKeybind"){
            PlayerPrefs.SetString("negYawKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "PitchKeybind"){
            PlayerPrefs.SetString("pitchKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "NegativePitchKeybind"){
            PlayerPrefs.SetString("negPitchKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "RollKeybind"){
            PlayerPrefs.SetString("rollKeybind", keyCode);
            PlayerPrefs.Save();
        }
        if (button.name == "NegativeRollKeybind"){
            PlayerPrefs.SetString("negRollKeybind", keyCode);
            PlayerPrefs.Save();
        }

        if (button.name == "FreezeKeybind"){
            PlayerPrefs.SetString("freezeKeybind", keyCode);
            PlayerPrefs.Save();
        }

    }

}