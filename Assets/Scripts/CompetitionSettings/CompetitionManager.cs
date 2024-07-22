using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompetitionManager : MonoBehaviour
{
	TaskSelection taskSelection;
	public Button competitionButton;
	public Text competitionButtonText;
	public AudioSource competitionAudioStart;
	public AudioSource competitionAudioEnd;
	public GameObject timeText;
	public GameObject scoreText;
	public GameObject pidUI;
	public GameObject taskSelectionUI;

	private float pauseMovementSeconds = 4f;

	private string buttonTextStart = "start comp";
	private string buttonTextEnd = "end comp";

	void Awake()
	{
		competitionButtonText.text = buttonTextStart;
	}

	void Start()
	{
		taskSelection = FindObjectOfType<TaskSelection>();
		competitionButton.onClick.AddListener(OnButtonClick);
	}

	void OnButtonClick()
	{
		// Change to Competition layout.
		taskSelection.current_option = 0;
		taskSelection.SetTaskEnvironment();

		if (competitionButtonText.text == buttonTextStart)
		{
			timeText.SetActive(true);
			scoreText.SetActive(true);
			pidUI.SetActive(false);
			taskSelectionUI.SetActive(false);

			// Run the StopMovementCoroutine method asynchronously (don't pause everything).
			StartCoroutine(PauseMovementCoroutine());
			competitionAudioStart.Play();

			CoinFlip.instance.StartScript();
			PassThroughGate.instance.StartScript();
			TimerCompetition.instance.StartScript();
			TricksChecker.instance.StartScript();
			Buoy.instance.StartScript();

			competitionButtonText.text = buttonTextEnd;
		}
		else
		{
			timeText.SetActive(false);
			scoreText.SetActive(false);
			pidUI.SetActive(true);
			taskSelectionUI.SetActive(false);

			competitionAudioEnd.Play();
			CoinFlip.instance.StopScript();
			PassThroughGate.instance.StopScript();
			TimerCompetition.instance.StopScript();
			TricksChecker.instance.StopScript();
			Buoy.instance.StopScript();

			competitionButtonText.text = buttonTextStart;
		}
	}

	private IEnumerator PauseMovementCoroutine()
	{
		Rigidbody auvRigidbody = taskSelection.Diana.GetComponent<Rigidbody>();
		auvRigidbody.isKinematic = true;
		yield return new WaitForSeconds(pauseMovementSeconds);
		auvRigidbody.isKinematic = false;
	}
}