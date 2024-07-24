using UnityEngine;

public class BinsManager : MonoBehaviour
{
	public static BinsManager instance;
	public int pointsAvailableCorrect;
	public int pointsAvailableWrong;

	private Bins[] binsArray;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		binsArray = FindObjectsOfType<Bins>();
	}

	public void StartAllBinsScripts()
	{
		foreach (Bins bin in binsArray)
		{
			bin.StartScript();
		}
	}

	public void StopAllBinsScripts()
	{
		foreach (Bins bin in binsArray)
		{
			bin.StopScript();
		}
	}
}
