using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Handles the Competition drawer UI: Top bar for selecting and starting competition missions
/// </summary>
public class CompetitionController : Controller
{
    private Label textTimer;
    private Label textScore;
    private DropdownField dropdownTask;
    private Button btnStartComp;

    
    public CompetitionController(VisualElement root)
    {
        QueryElements(root);
        RegisterCallbacks();
    }
    
    protected override void QueryElements(VisualElement root)
    {
        textTimer = root.Q<Label>("Text-Timer");
        textScore = root.Q<Label>("Text-Score");
        dropdownTask = root.Q<DropdownField>("Dropdown-Task");
        // TODO: Populate dropdown with actual task presets from TaskSelection and query from planner if needed
        dropdownTask.choices = new List<string>()
        {
            "Task 1: Pre-qualification",
            "Task 2: Gate Task",
            "Task 3: Bin Task"
        };
        btnStartComp = root.Q<Button>("Btn-StartComp");

        
    }
    
    protected override void RegisterCallbacks()
    {
        // TODO: Implement mission start logic based on selected task
    }
    
    public void SetTime(float secondsElapsed)
    {
        if (textTimer != null)
        {
            int minutes = Mathf.FloorToInt(secondsElapsed / 60F);
            int seconds = Mathf.FloorToInt(secondsElapsed % 60F);
            string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

            textTimer.text = timeStr;
        }
    }
    public void SetScore(int score)
    {
        if (textScore != null)
        {
            textScore.text = score.ToString();
        }
    }
}
