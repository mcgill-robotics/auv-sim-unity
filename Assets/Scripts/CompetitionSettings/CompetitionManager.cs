using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompetitionManager : MonoBehaviour {
	TaskSelection taskSelection;
	public Button competitionButton;
	public Text competitionButtonText;
	public AudioSource competitionAudioStart;
	public AudioSource competitionAudioEnd;
	private float pauseMovementSeconds = 4f;
	
	private string buttonTextStart = "start comp";
	private string buttonTextEnd = "end comp"; 

	void Awake() {
		competitionButtonText.text = buttonTextStart;
	}

	void Start() {
		taskSelection = FindObjectOfType<TaskSelection>();
		competitionButton.onClick.AddListener(OnButtonClick);
	}

	void OnButtonClick() {
		// Change to Competition layout.
		taskSelection.current_option = 0;
		taskSelection.SetTaskEnvironment();

		if (competitionButtonText.text == buttonTextStart) {
			// Run the StopMovementCoroutine method asynchronously (don't pause everything).
			StartCoroutine(PauseMovementCoroutine());
			competitionAudioStart.Play();
			
			FacingChecker.instance.StartScript();
			PassThroughGate.instance.StartScript();
			TimerCompetition.instance.StartScript();

			competitionButtonText.text = buttonTextEnd;
		} else {
			competitionAudioEnd.Play();
			FacingChecker.instance.StopScript();
			PassThroughGate.instance.StopScript();
			TimerCompetition.instance.StopScript();
		
			competitionButtonText.text = buttonTextStart;
		}
	}
	
	private IEnumerator PauseMovementCoroutine() {
		Rigidbody auvRigidbody = taskSelection.Diana.GetComponent<Rigidbody>();
		auvRigidbody.isKinematic = true;
		yield return new WaitForSeconds(pauseMovementSeconds);
		auvRigidbody.isKinematic = false;
	}
}