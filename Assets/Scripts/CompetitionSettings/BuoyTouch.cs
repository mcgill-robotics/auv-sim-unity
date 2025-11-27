using UnityEngine;
using System.Collections;

public class BuoyTouch : MonoBehaviour, ICompetitionTask
{
    public static BuoyTouch instance;
    public int pointsAvailable;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        this.enabled = false;
    }

    public void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.name == "Diana")
        {
            PointsManager.instance.AddPoint(pointsAvailable, "Buoy");
            MessageBox.instance.AddMessage(string.Format("Buoy Touch +{0}pts", pointsAvailable));
            StopScript();
        }
    }

    public void StartScript()
    {
        this.enabled = true;
    }

    public void StopScript()
    {
        this.enabled = false;
    }
}