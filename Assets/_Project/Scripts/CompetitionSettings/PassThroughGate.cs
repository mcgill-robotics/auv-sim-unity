using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Competition task: Detects when AUV passes through the gate and sets team color based on which side.
/// </summary>
public class PassThroughGate : MonoBehaviour, ICompetitionTask
{
    public static PassThroughGate instance;
    public Transform gate;
    public Transform gatePole1;
    public Transform gatePole2;
    public Transform blueSign;
    public Transform redSign;
    public Transform auv;
    public int pointsAvailable;
    private bool hasEnteredGate = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        this.enabled = false; // Start disabled.
    }

    void Update()
    {
        CheckGatePassage();
    }

    void CheckGatePassage()
    {
        if (IsWithinGateBounds())
        {
            if (!hasEnteredGate)
            {
                hasEnteredGate = true;
            }
        }
        else
        {
            if (hasEnteredGate)
            {
                hasEnteredGate = false;
                PointsManager.instance.AddPoint(pointsAvailable, "Gate");
                MessageBox.instance.AddMessage(string.Format("Gate Pass Through +{0}pts", pointsAvailable));
                SetColor();
                StopScript();
            }
        }
    }

    bool IsWithinGateBounds()
    {
        // Gate as rectangle (2D) that you have to go through.
        Vector3 auvPosition = auv.position;
        Vector3 gatePosition = gate.position;
        Vector3 pole1Position = gatePole1.position;
        Vector3 pole2Position = gatePole2.position;

        // Z axis check (unity).
        bool withinZBounds = auvPosition.z <= Math.Max(pole1Position.z, pole2Position.z) && auvPosition.z >= Math.Min(pole1Position.z, pole2Position.z);

        // X axis check (unity).
        bool withinXBounds = auvPosition.x <= Math.Max(pole1Position.x, pole2Position.x) && auvPosition.x >= Math.Min(pole1Position.x, pole2Position.x);

        // Y axis check (unity).
        bool withinYBounds = auvPosition.y <= gatePosition.y;

        return withinZBounds && withinXBounds && withinYBounds;
    }

    void SetColor()
    {
        float distanceToRed = Vector3.Distance(auv.position, redSign.position);
        float distanceToBlue = Vector3.Distance(auv.position, blueSign.position);
        if (distanceToBlue < distanceToRed)
        {
            PointsManager.instance.color = "blue";
            MessageBox.instance.AddMessage("Setting competition color to BLUE");
        }
        else
        {
            PointsManager.instance.color = "red";
            MessageBox.instance.AddMessage("Setting competition color to RED");
        }
    }

    public void StartScript()
    {
        this.enabled = true; // Enable the script.
    }

    public void StopScript()
    {
        this.enabled = false; // Disable the script.
    }
}
