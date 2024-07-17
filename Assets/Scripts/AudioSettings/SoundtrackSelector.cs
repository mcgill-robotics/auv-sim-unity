using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class BackgroundMusicManager : MonoBehaviour {
	public TMP_Dropdown audioDropdown;
	public AudioSource audioSource;
	public List<AudioClip> audioClips;
	private List<string> audioOptions = new List<string>();
	private int lastAudioIndex;

	void Start() {
		// Ensure the AudioSource component is attached
		if (audioSource == null) {
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		foreach (AudioClip clip in audioClips) {
			audioOptions.Add(clip.name);
		}
		audioOptions.Add("Silence");

		// Initialize the dropdown options.
		audioDropdown.AddOptions(audioOptions);

		// Start playing default selection.
		audioSource.clip = audioClips[0];
		audioSource.Play();

		lastAudioIndex = 0;

		// Set up the listener for the dropdown.
		audioDropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(audioDropdown); });
	}

	void DropdownValueChanged(TMP_Dropdown change) {
		int index = change.value;

		if (index >= 0 && index < audioClips.Count) {
			audioSource.clip = audioClips[index];
			audioSource.Play();
			lastAudioIndex = index;
		} else {
			audioSource.clip = audioClips[lastAudioIndex];
			audioSource.Pause();
		}
	}
}
