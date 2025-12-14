using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages competition mode - handles starting/stopping runs, UI visibility, and task scoring.
/// </summary>
public class CompetitionManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Button to start/stop competition")]
    public Button competitionButton;
    
    [Tooltip("Text label on the competition button")]
    public Text competitionButtonText;
    
    [Tooltip("Sound played when competition starts")]
    public AudioSource competitionAudioStart;
    
    [Tooltip("Sound played when competition ends")]
    public AudioSource competitionAudioEnd;
    
    [Tooltip("Timer display during competition")]
    public GameObject timeText;
    
    [Tooltip("Score display during competition")]
    public GameObject scoreText;
    
    [Tooltip("PID control panel (hidden during competition)")]
    public GameObject pidUI;
    
    [Tooltip("Task selection panel (hidden during competition)")]
    public GameObject taskSelectionUI;
    
    [Tooltip("Message box for task feedback")]
    public GameObject messageBox;

    [Header("Competition Tasks")]
    [Tooltip("List of scripts implementing ICompetitionTask interface")]
    public List<MonoBehaviour> competitionTasks;

    private float pauseMovementSeconds = 4f;
    private string buttonTextStart = "start comp";
    private string buttonTextEnd = "end comp";
    
    [SerializeField] 
    [Tooltip("Reference to TaskSelection component for environment setup")]
    private TaskSelection taskSelection;

    void Awake()
    {
        // competitionButtonText.text = buttonTextStart;
    }

    void Start()
    {
        if (taskSelection == null)
        {
            Debug.LogError("[CompetitionManager] TaskSelection not assigned. Please assign it in the Inspector.");
            return;
        }
        
        competitionButton.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        taskSelection.current_option = 0;
        taskSelection.SetTaskEnvironment();

        if (competitionButtonText.text == buttonTextStart)
        {
            StartCompetition();
        }
        else
        {
            EndCompetition();
        }
    }

    private void StartCompetition()
    {
        timeText.SetActive(true);
        scoreText.SetActive(true);
        pidUI.SetActive(false);
        taskSelectionUI.SetActive(false);
        messageBox.SetActive(true);

        StartCoroutine(PauseMovementCoroutine());
        competitionAudioStart.Play();

        foreach (var task in competitionTasks)
        {
            if (task is ICompetitionTask competitionTask)
            {
                competitionTask.StartScript();
            }
        }

        competitionButtonText.text = buttonTextEnd;
    }

    private void EndCompetition()
    {
        timeText.SetActive(false);
        scoreText.SetActive(false);
        pidUI.SetActive(true);
        taskSelectionUI.SetActive(true);
        messageBox.SetActive(false);

        competitionAudioEnd.Play();

        foreach (var task in competitionTasks)
        {
            if (task is ICompetitionTask competitionTask)
            {
                competitionTask.StopScript();
            }
        }

        competitionButtonText.text = buttonTextStart;
    }

    private IEnumerator PauseMovementCoroutine()
    {
        Rigidbody auvRigidbody = taskSelection.Diana.GetComponent<Rigidbody>();
        auvRigidbody.isKinematic = true;
        yield return new WaitForSeconds(pauseMovementSeconds);
        auvRigidbody.isKinematic = false;
    }
}