using UnityEngine;
using System.Collections;
using TMPro;

public class TimerCompetition : MonoBehaviour
{
	public static TimerCompetition instance;
	public TMP_Text timerText;
	private float timer = 0f;
	private float timerStartDelay = 4f; // Depends on the countdown audio.
	private bool isCounting = false;

	void Awake()
	{
		instance = this;
		timerText.text = "00:00";
	}

	void Start()
	{
		this.enabled = false;
	}

	void Update()
	{
		if (isCounting)
		{
			timer += Time.deltaTime;

			int minutes = Mathf.FloorToInt(timer / 60F);
			int seconds = Mathf.FloorToInt(timer % 60F);

			timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
		}
	}

	public void ResetTimer()
	{
		timer = 0f;
	}

	public void StartScript()
	{
		this.enabled = true; // Enable the script.
		StartCoroutine(StartTimerWithDelay(timerStartDelay)); // Start coroutine with 4-second delay. 
	}

	public void StopScript()
	{
		this.enabled = false; // Disable the script.
	}

	private IEnumerator StartTimerWithDelay(float delay)
	{
		yield return new WaitForSeconds(delay); // Wait for the specified delay.
		isCounting = true; // Start counting after the delay.
	}
}
