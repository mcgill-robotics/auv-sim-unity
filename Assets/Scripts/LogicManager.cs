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
	public string missionStatusTopicName;
	public string DVLStatusTopicName;
	public string IMUStatusTopicName;
	public string depthStatusTopicName;
	public string hydrophonesStatusTopicName;
	public string actuatorStatusTopicName;
	public string downCamStatusTopicName;
	public string frontCamStatusTopicName;
	public string IMUFrontCamStatusTopicName;
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
	public TMP_Text MissionStatusText;
	public TMP_Text DVLStatusText;
	public TMP_Text IMUStatusText;
	public TMP_Text DepthStatusText;
	public TMP_Text HydrophonesStatusText;
	public TMP_Text ActuatorStatusText;
	public TMP_Text FrontCamStatusText;
	public TMP_Text DownCamStatusText;
	public TMP_Text IMUFrontCamStatusText;

	[Header("FOR QUALITY SETTINGS")]
	public TMP_Dropdown qualityDropdown;
	public GameObject waterObject;

	[Header("FOR PUBLISHER TOGGLE SETTINGS")]
	public Toggle PublishDVLToggle;
	public Toggle PublishROSToggle;
	public Toggle DisplaySimToggle;
	public Toggle PublishIMUToggle;
	public Toggle PublishDepthToggle;
	public Toggle PublishHydrophonesToggle;
	public Toggle PublishFrontCamToggle;
	public Toggle PublishDownCamToggle;
	public Toggle VisualizeBearing1Toggle;
	public Toggle VisualizeBearing2Toggle;
	public Toggle VisualizeBearing3Toggle;
	public Toggle VisualizeBearing4Toggle;

	public LayerMask hiddenSimLayerMask;
	private LayerMask followCamDefaultLayerMask;
	private LayerMask freeCamDefaultLayerMask;

	public TMP_Dropdown HydrophonesNumberDropdown;

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
	public StatePublisher statePub;
	public CameraPublisher frontCamPub;
	public CameraPublisher downCamPub;
	public CameraDepthPublisher depthCamPub;
	public int hydrophonesNumberOption;


	private void Start()
	{
		roscon = ROSConnection.GetOrCreateInstance();
		roscon.Subscribe<Float64Msg>(xPositionTopicName, xPositionCallback);
		roscon.Subscribe<Float64Msg>(yPositionTopicName, yPositionCallback);
		roscon.Subscribe<Float64Msg>(zPositionTopicName, zPositionCallback);
		roscon.Subscribe<Float64Msg>(thetaXTopicName, thetaXCallback);
		roscon.Subscribe<Float64Msg>(thetaYTopicName, thetaYCallback);
		roscon.Subscribe<Float64Msg>(thetaZTopicName, thetaZCallback);
		roscon.Subscribe<StringMsg>(missionStatusTopicName, missionStatusCallback);
		roscon.Subscribe<Int32Msg>(DVLStatusTopicName, DVLStatusCallback);
		roscon.Subscribe<Int32Msg>(IMUStatusTopicName, IMUStatusCallback);
		roscon.Subscribe<Int32Msg>(depthStatusTopicName, depthStatusCallback);
		roscon.Subscribe<Int32Msg>(hydrophonesStatusTopicName, hydrophonesStatusCallback);
		roscon.Subscribe<Int32Msg>(actuatorStatusTopicName, actuatorStatusCallback);
		roscon.Subscribe<Int32Msg>(downCamStatusTopicName, downCamStatusCallback);
		roscon.Subscribe<Int32Msg>(frontCamStatusTopicName, frontCamStatusCallback);
		roscon.Subscribe<Int32Msg>(IMUFrontCamStatusTopicName, IMUFrontCamStatusCallback);
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
		hydrophonesNumberOption = 0;
		HydrophonesNumberDropdown.onValueChanged.AddListener(delegate
		{
			DropdownValueChanged(HydrophonesNumberDropdown);
		});
	}

	void DropdownValueChanged(TMP_Dropdown change)
	{
		hydrophonesNumberOption = change.value;
	}

	void xPositionCallback(Float64Msg msg)
	{
		XPosText.text = "Current X: " + msg.data;
	}

	void yPositionCallback(Float64Msg msg)
	{
		YPosText.text = "Current Y: " + msg.data;
	}

	void zPositionCallback(Float64Msg msg)
	{
		ZPosText.text = "Current Z: " + msg.data;
	}

	void thetaXCallback(Float64Msg msg)
	{
		RotXText.text = "Current Euler X: " + msg.data;
	}

	void thetaYCallback(Float64Msg msg)
	{
		RotYText.text = "Current Euler Y: " + msg.data;
	}

	void thetaZCallback(Float64Msg msg)
	{
		RotZText.text = "Current Euler Z: " + msg.data;
	}

	void missionStatusCallback(StringMsg msg)
	{
		MissionStatusText.text = "Mission status: " + msg.data;
	}

	void DVLStatusCallback(Int32Msg msg)
	{
		DVLStatusText.text = "DVL Status: " + msg.data.ToString();
	}

	void IMUStatusCallback(Int32Msg msg)
	{
		IMUStatusText.text = "IMU Status: " + msg.data.ToString();
	}

	void depthStatusCallback(Int32Msg msg)
	{
		DepthStatusText.text = "Depth Status: " + msg.data.ToString();
	}

	void hydrophonesStatusCallback(Int32Msg msg)
	{
		HydrophonesStatusText.text = "Hydrophones Status: " + msg.data.ToString();
	}

	void actuatorStatusCallback(Int32Msg msg)
	{
		ActuatorStatusText.text = "Actuator Status: " + msg.data.ToString();
	}

	void downCamStatusCallback(Int32Msg msg)
	{
		DownCamStatusText.text = "Down Cam Status: " + msg.data.ToString();
	}

	void frontCamStatusCallback(Int32Msg msg)
	{
		FrontCamStatusText.text = "Front Cam Status: " + msg.data.ToString();
	}

	void IMUFrontCamStatusCallback(Int32Msg msg)
	{
		IMUFrontCamStatusText.text = "IMU Front Cam Status: " + msg.data.ToString();
	}

	public void activateDownCam()
	{
		downCam.SetActive(true);
		frontCam.SetActive(false);
		freeCam.SetActive(false);
		followCam.SetActive(false);
		depthCam.SetActive(false);
	}

	public void activateFrontCam()
	{
		downCam.SetActive(false);
		frontCam.SetActive(true);
		freeCam.SetActive(false);
		followCam.SetActive(false);
		depthCam.SetActive(false);
	}

	public void activateFreeCam()
	{
		downCam.SetActive(false);
		frontCam.SetActive(false);
		freeCam.SetActive(true);
		followCam.SetActive(false);
		depthCam.SetActive(false);
	}

	public void activateFollowCam()
	{
		downCam.SetActive(false);
		frontCam.SetActive(false);
		freeCam.SetActive(false);
		followCam.SetActive(true);
		depthCam.SetActive(false);
	}

	public void activateDepthCam()
	{
		downCam.SetActive(false);
		frontCam.SetActive(false);
		freeCam.SetActive(false);
		followCam.SetActive(false);
		depthCam.SetActive(true);
	}

	public void snapFreeCam()
	{
		if (freeCam.activeSelf)
		{
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

	public void SetXPID()
	{
		BoolMsg bool_msg = new BoolMsg(true);
		roscon.Publish(pidXEnableName, bool_msg);

		if (float.TryParse(xInputField.text, out float result))
		{
			Float64Msg msg = new Float64Msg();
			msg.data = float.Parse(xInputField.text);
			roscon.Publish(xSetpointTopicName, msg);
		}
		else
		{
			Debug.LogWarning("Invalid X PID input");
		}
	}

	public void SetYPID()
	{
		BoolMsg bool_msg = new BoolMsg(true);
		roscon.Publish(pidYEnableName, bool_msg);

		if (float.TryParse(yInputField.text, out float result))
		{
			Float64Msg msg = new Float64Msg();
			msg.data = float.Parse(yInputField.text);
			roscon.Publish(ySetpointTopicName, msg);
		}
		else
		{
			Debug.LogWarning("Invalid Y PID input");
		}
	}

	public void SetZPID()
	{
		BoolMsg bool_msg = new BoolMsg(true);
		roscon.Publish(pidZEnableName, bool_msg);

		if (float.TryParse(zInputField.text, out float result))
		{
			Float64Msg msg = new Float64Msg();
			msg.data = float.Parse(zInputField.text);
			roscon.Publish(zSetpointTopicName, msg);
		}
		else
		{
			Debug.LogWarning("Invalid Z PID input");
		}
	}

	public void SetQuatPID()
	{
		BoolMsg bool_msg = new BoolMsg(true);
		roscon.Publish(pidQuatEnableName, bool_msg);

		if (float.TryParse(rotXInputField.text, out float resultX) &&
				float.TryParse(rotYInputField.text, out float resultY) &&
				float.TryParse(rotZInputField.text, out float resultZ))
		{
			RosMessageTypes.Geometry.QuaternionMsg msg = new RosMessageTypes.Geometry.QuaternionMsg();
			Quaternion rollQuaternion = Quaternion.Euler(0f, 0f, -resultX);
			Quaternion pitchQuaternion = Quaternion.Euler(-resultY, 0f, 0f);
			Quaternion yawQuaternion = Quaternion.Euler(0f, resultZ, 0f);
			Quaternion setpoint = rollQuaternion * pitchQuaternion * yawQuaternion; // to specify XYZ order of euler angles

			msg = setpoint.To<NED>();
			roscon.Publish(quatSetpointTopicName, msg);
		}
		else
		{
			Debug.LogWarning("Invalid Rotation PID");
		}
	}

	public void SetQualityLevel()
	{
		QualitySettings.SetQualityLevel(qualityDropdown.value, true);
		PlayerPrefs.SetString("qualityLevel", qualityDropdown.value.ToString());
		PlayerPrefs.Save();
		if (qualityDropdown.value == 3)
		{ //turn off water on barebones
			waterObject.SetActive(false);
		}
		else
		{
			waterObject.SetActive(true);
		}
	}

	public void SetROSPublishToggle()
	{
		PlayerPrefs.SetString("PublishROSToggle", PublishROSToggle.isOn.ToString());
		PlayerPrefs.Save();
		downCamPub.SetPublishToRos(PublishDownCamToggle.isOn && PublishROSToggle.isOn);
		frontCamPub.SetPublishToRos(PublishFrontCamToggle.isOn && PublishROSToggle.isOn);
		depthCamPub.SetPublishToRos(PublishDepthToggle.isOn && PublishROSToggle.isOn);
	}

	public void hideSimObjects()
	{
		if (DisplaySimToggle.isOn)
		{
			freeCam.GetComponent<Camera>().cullingMask = freeCamDefaultLayerMask;
			followCam.GetComponent<Camera>().cullingMask = followCamDefaultLayerMask;
		}
		else
		{
			freeCam.GetComponent<Camera>().cullingMask = hiddenSimLayerMask;
			followCam.GetComponent<Camera>().cullingMask = hiddenSimLayerMask;
		}
		PlayerPrefs.SetString("DisplaySimToggle", DisplaySimToggle.isOn.ToString());
		PlayerPrefs.Save();
	}


	public void SetPublishDVLToggle()
	{
		PlayerPrefs.SetString("PublishDVLToggle", PublishDVLToggle.isOn.ToString());
		PlayerPrefs.Save();
	}

	public void SetPublishIMUToggle()
	{
		PlayerPrefs.SetString("PublishIMUToggle", PublishIMUToggle.isOn.ToString());
		PlayerPrefs.Save();
	}

	public void SetPublishDepthToggle()
	{
		PlayerPrefs.SetString("PublishDepthToggle", PublishDepthToggle.isOn.ToString());
		PlayerPrefs.Save();
		depthCamPub.SetPublishToRos(PublishDepthToggle.isOn && PublishROSToggle.isOn);
	}

	public void SetPublishHydrophonesToggle()
	{
		PlayerPrefs.SetString("PublishHydrophonesToggle", PublishHydrophonesToggle.isOn.ToString());
		PlayerPrefs.Save();
	}
	public void SetPublishFrontCamToggle()
	{
		PlayerPrefs.SetString("PublishFrontCamToggle", PublishFrontCamToggle.isOn.ToString());
		PlayerPrefs.Save();
		frontCamPub.SetPublishToRos(PublishFrontCamToggle.isOn && PublishROSToggle.isOn);
	}

	public void SetPublishDownCamToggle()
	{
		PlayerPrefs.SetString("PublishDownCamToggle", PublishDownCamToggle.isOn.ToString());
		PlayerPrefs.Save();
		downCamPub.SetPublishToRos(PublishDownCamToggle.isOn && PublishROSToggle.isOn);
	}

	public void OnButtonClick(Button clickedButton)
	{
		if (!isCheckingKey)
		{
			Image buttonImage = clickedButton.GetComponent<Image>();

			if (buttonImage != null)
			{
				originalButtonColor = buttonImage.color;
			}

			ChangeButtonColor(clickedButton, new Color(1f, 0f, 0f, 0.5f));
			StartCoroutine(CheckForKey(clickedButton));
		}
	}

	public void OnClickRates(Button clickedButton)
	{
		if (clickedButton.name == "SetFrontCamRateBtn")
		{
			PlayerPrefs.SetString("frontCamRate", frontCamRateInputField.text);
			frontCamPub.SetPublishRate(int.Parse(frontCamRateInputField.text));
			depthCamPub.SetPublishRate(int.Parse(frontCamRateInputField.text));
		}
		if (clickedButton.name == "SetDownCamRateBtn")
		{
			PlayerPrefs.SetString("downCamRate", downCamRateInputField.text);
			downCamPub.SetPublishRate(int.Parse(downCamRateInputField.text));
		}
		if (clickedButton.name == "SetFrontCamResBtn")
		{
			PlayerPrefs.SetString("frontCamWidth", frontCamWidthInputField.text);
			PlayerPrefs.SetString("frontCamHeight", frontCamHeightInputField.text);
			frontCamPub.SetPublishResolution(int.Parse(frontCamWidthInputField.text), int.Parse(frontCamHeightInputField.text));
			depthCamPub.SetPublishResolution(int.Parse(frontCamWidthInputField.text), int.Parse(frontCamHeightInputField.text));
		}
		if (clickedButton.name == "SetDownCamResBtn")
		{
			PlayerPrefs.SetString("downCamHeight", downCamHeightInputField.text);
			PlayerPrefs.SetString("downCamWidth", downCamWidthInputField.text);
			downCamPub.SetPublishResolution(int.Parse(downCamHeightInputField.text), int.Parse(downCamWidthInputField.text));
		}
		if (clickedButton.name == "SetPoseRateBtn")
		{
			PlayerPrefs.SetString("poseRate", poseRateInputField.text);
			statePub.SetPublishRate(int.Parse(poseRateInputField.text));
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
						TextMeshProUGUI buttonText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
						string newText = keyCode.ToString().ToLower();
						buttonText.text = newText;
						isCheckingKey = false;
						SetPlayerRefsKeys(clickedButton, newText);
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

	private void SetPlayerRefsKeys(Button button, string keyCode)
	{
		// movement keybinds
		if (button.name == "ForwardKeybind")
		{
			PlayerPrefs.SetString("surgeKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "BackwardKeybind")
		{
			PlayerPrefs.SetString("negSurgeKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "LeftKeybind")
		{
			PlayerPrefs.SetString("swayKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "RightKeybind")
		{
			PlayerPrefs.SetString("negSwayKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "HeaveKeybind")
		{
			PlayerPrefs.SetString("heaveKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "NegativeHeaveKeybind")
		{
			PlayerPrefs.SetString("negHeaveKeybind", keyCode);
			PlayerPrefs.Save();
		}

		// rotations keybinds
		if (button.name == "YawKeybind")
		{
			PlayerPrefs.SetString("yawKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "NegativeYawKeybind")
		{
			PlayerPrefs.SetString("negYawKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "PitchKeybind")
		{
			PlayerPrefs.SetString("pitchKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "NegativePitchKeybind")
		{
			PlayerPrefs.SetString("negPitchKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "RollKeybind")
		{
			PlayerPrefs.SetString("rollKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "NegativeRollKeybind")
		{
			PlayerPrefs.SetString("negRollKeybind", keyCode);
			PlayerPrefs.Save();
		}
		if (button.name == "FreezeKeybind")
		{
			PlayerPrefs.SetString("freezeKeybind", keyCode);
			PlayerPrefs.Save();
		}
	}
}