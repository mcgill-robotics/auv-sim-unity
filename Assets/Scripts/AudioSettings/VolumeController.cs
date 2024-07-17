using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour {
	public Slider volumeSlider;
	private AudioSource[] allAudioSources;

	void Start() {
		// Find all AudioSources in the scene.
		allAudioSources = FindObjectsOfType<AudioSource>();

		// Initialize the slider's value to the volume of the first AudioSource.
		if (allAudioSources.Length > 0 && volumeSlider != null) {
			volumeSlider.value = allAudioSources[0].volume;
			volumeSlider.onValueChanged.AddListener(SetVolume);
		}
	}

	public void SetVolume(float volume) {
		foreach (AudioSource audioSource in allAudioSources) {
			audioSource.volume = volume;
		}
	}
}
