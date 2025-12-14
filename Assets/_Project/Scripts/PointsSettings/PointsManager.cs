using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton that tracks competition score and team color (red/blue for buoy direction).
/// </summary>
public class PointsManager : MonoBehaviour
{
    public static PointsManager instance;
    public string color = "none";
    private int currentScore = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddPoint(int points, string taskName)
    {
        currentScore += points;
        if (SimulatorHUD.Instance != null)
        {
            SimulatorHUD.Instance.UpdateScore(currentScore.ToString());
            SimulatorHUD.Instance.Log($"Task Complete: {taskName} (+{points} pts)");
        }
    }

    public void ResetPoints()
    {
        currentScore = 0;
        if (SimulatorHUD.Instance != null)
        {
            SimulatorHUD.Instance.UpdateScore("0");
        }
    }
}
