using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompetitionManager : MonoBehaviour
{
    public Button competitionButton;
    public Text competitionButtonText;
    public AudioSource competitionAudioStart;
    public AudioSource competitionAudioEnd;
    public GameObject timeText;
    public GameObject scoreText;
    public GameObject pidUI;
    public GameObject taskSelectionUI;
    public GameObject messageBox;

    public List<MonoBehaviour> competitionTasks; // Assign scripts that implement ICompetitionTask here

    private float pauseMovementSeconds = 4f;
    private string buttonTextStart = "start comp";
    private string buttonTextEnd = "end comp";
    [SerializeField] private TaskSelection taskSelection;

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