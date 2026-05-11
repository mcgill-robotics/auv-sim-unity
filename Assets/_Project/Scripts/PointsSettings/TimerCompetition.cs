using UnityEngine;
using System.Collections;

public class TimerCompetition : MonoBehaviour
{
    public static TimerCompetition instance;
    
    private float currentTime = 0f;
    private bool isRunning = false;

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

    void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        currentTime = 0f;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        isRunning = false;
        currentTime = 0f;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (SimulatorHUD.Instance != null)
        {
            SimulatorHUD.Instance.UpdateTimer(currentTime);
        }
    }
}
