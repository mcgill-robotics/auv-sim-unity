using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
	public Slider volumeSlider;
	public AudioSource soundtrack;

	void Start()
	{
		volumeSlider.value = soundtrack.volume;
		volumeSlider.onValueChanged.AddListener(SetVolume);
	}

	public void SetVolume(float volume)
	{
		soundtrack.volume = volume / 2;
	}
}