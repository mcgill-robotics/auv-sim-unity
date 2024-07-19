using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BackgroundMusicManager : MonoBehaviour {
	public TMP_Dropdown audioDropdown;
	public AudioSource audioSource;
	
	public List<AudioClip> defaultSoundtracks;
	public List<AudioClip> rapSoundtacks; 
	public List<AudioClip> popSoundtacks;
	public List<AudioClip> brazilianSoundtacks;
	
	private List<string> defaultOptions = new List<string>();
	private List<string> competitionSoundtrackGenres = new List<string>() {
		"Rap", "Pop", "Brazilian", "Silence"
	};

	private List<AudioClip> currentTracks = new List<AudioClip>();

	private int currentTrackIndex;
	private bool isCompetition = false;
	private Coroutine playNextTrackCoroutine;

	void Start() {
		// Ensure the AudioSource component is attached
		if (audioSource == null) {
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		foreach (AudioClip clip in defaultSoundtracks) {
			defaultOptions.Add(clip.name);
		}
		defaultOptions.Add("Silence");

		// Initialize the dropdown options.
		audioDropdown.AddOptions(defaultOptions);

		// Start playing default selection.
		audioSource.clip = defaultSoundtracks[0];
		audioSource.volume = audioSource.volume / 4; 
		audioSource.Play();

		currentTrackIndex = 0;

		// Set up the listener for the dropdown.
		audioDropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(audioDropdown); });
	}

	public void ChangeAudioOptions() {
		audioDropdown.ClearOptions();
		if (!isCompetition) {
			audioDropdown.AddOptions(competitionSoundtrackGenres);
			isCompetition = true;
			PlaySelectedTracks(rapSoundtacks);
		} else {
			audioDropdown.AddOptions(defaultOptions);
			isCompetition = false;
			PlaySelectedTracks(defaultSoundtracks);
		}
	}	

	void DropdownValueChanged(TMP_Dropdown change) {
		if (isCompetition) {
			switch (competitionSoundtrackGenres[change.value]) {
				case "Rap":
					PlaySelectedTracks(rapSoundtacks);
					break;
				case "Pop":
					PlaySelectedTracks(popSoundtacks);
					break;
				case "Brazilian":
					PlaySelectedTracks(brazilianSoundtacks);
					break;
				case "Silence":
					StopPlaying();
					break;
			}
		} else {
			PlaySelectedTracks(new List<AudioClip> { defaultSoundtracks[change.value] });
		}
	}

	void PlaySelectedTracks(List<AudioClip> tracks) {
		currentTracks = new List<AudioClip>(tracks);
		currentTrackIndex = 0;
		PlayNextTrack();
	}

	void PlayNextTrack() {
		audioSource.clip = currentTracks[currentTrackIndex];
		audioSource.Play();
		currentTrackIndex = (currentTrackIndex + 1) % currentTracks.Count;
		// Stop any previously running coroutine to ensure only one is running at a time.
		if (playNextTrackCoroutine != null) {
			StopCoroutine(playNextTrackCoroutine);
		}
		// Start the coroutine to wait for the current track to end.
		playNextTrackCoroutine = StartCoroutine(WaitForTrackToEnd());
	}

	IEnumerator WaitForTrackToEnd() {
		yield return new WaitWhile(() => audioSource.isPlaying);
		PlayNextTrack();
	}

	public void StopPlaying() {
		if (audioSource.isPlaying) {
			audioSource.Stop();
		}

		// Stop the coroutine if it is running.
		if (playNextTrackCoroutine != null) {
			StopCoroutine(playNextTrackCoroutine);
			playNextTrackCoroutine = null;
		}
	}
}
