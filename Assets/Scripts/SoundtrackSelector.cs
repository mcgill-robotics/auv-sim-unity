using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class BackgroundMusicManager : MonoBehaviour {
	public TMP_Dropdown audioDropdown;
	public AudioSource audioSource;
	public List<AudioClip> audioClips;
	private List<string> audioOptions = new List<string>();

	void Start() {
		// // Get the TMP_Dropdown component attached to the same GameObject
		// audioDropdown = GetComponent<TMP_Dropdown>();

		// Ensure the AudioSource component is attached
		if (audioSource == null) {
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		foreach (AudioClip clip in audioClips) {
			audioOptions.Add(clip.name);
		}

		// Initialize the dropdown options.
		audioDropdown.AddOptions(audioOptions);

		// Start playing default selection.
		audioSource.clip = audioClips[0];
		audioSource.Play();

		// Set up the listener for the dropdown.
		audioDropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(audioDropdown); });
	}

	void DropdownValueChanged(TMP_Dropdown change) {
		int index = change.value;

		if (index >= 0 && index < audioClips.Count) {
			audioSource.clip = audioClips[index];
			audioSource.Play();
		}
	}
}
