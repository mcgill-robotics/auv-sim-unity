using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class PointsManager : MonoBehaviour
{
	public static PointsManager instance;
	public TMP_Text scoreText;
	public AudioSource[] pointSequenceAudio;
	private int total_score = 0;
	private Dictionary<string, int> tasksAudioIndex = new Dictionary<string, int> {
		{"Gate", 0},
		{"Buoy", 0},
		{"Bins", 0},
		{"Torpedo", 0},
		{"Octagon", 0},
		{"Pinger", 0}
	};

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		scoreText.text = "SCORE: " + total_score.ToString();
	}

	public void AddPoint(int points, string task)
	{
		if (!tasksAudioIndex.ContainsKey(task))
		{
			Debug.Log(string.Format("[PointsManager.cs] {0} is NOT a valid task!", task));
			return;
		}
		int audioIndex = tasksAudioIndex[task] >= pointSequenceAudio.Length ? pointSequenceAudio.Length - 1 : tasksAudioIndex[task];
		tasksAudioIndex[task]++;
		pointSequenceAudio[audioIndex].Play();
		total_score += points;
		scoreText.text = "SCORE: " + total_score.ToString();
	}

	public void ResetPoint()
	{
		total_score = 0;
		scoreText.text = "SCORE: " + total_score.ToString();
	}
}
