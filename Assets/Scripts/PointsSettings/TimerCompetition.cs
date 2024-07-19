using UnityEngine;
using TMPro;

public class TimerCompetition : MonoBehaviour {
	public static TimerCompetition instance; 
	public TMP_Text timerText;
	private float timer = 0f;

	void Awake() {
		instance = this;
	}

	void Start() {
		this.enabled = false; // Start disabled.
	}

	void Update() {
		timer += Time.deltaTime;

		int minutes = Mathf.FloorToInt(timer / 60F);
		int seconds = Mathf.FloorToInt(timer % 60F);

		timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
	}
	public void ResetTimer() {
		timer = 0f;
	}

	public void StartScript() {
		this.enabled = true; // Enable the script.
	}

	public void StopScript() {
		ResetTimer();
		this.enabled = false; // Disable the script.
	}
}
