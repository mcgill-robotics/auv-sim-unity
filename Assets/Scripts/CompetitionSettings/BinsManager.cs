using UnityEngine;

public class BinsManager : MonoBehaviour, ICompetitionTask
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
        binsArray = FindObjectsByType<Bins>(FindObjectsSortMode.None);
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

    public void StartScript()
    {
        StartAllBinsScripts();
    }

    public void StopScript()
    {
        StopAllBinsScripts();
    }
}
