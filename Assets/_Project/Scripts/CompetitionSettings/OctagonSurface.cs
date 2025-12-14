using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Competition task: Awards points when AUV surfaces inside the octagon area.
/// </summary>
public class OctagonSurface : MonoBehaviour, ICompetitionTask
{
    public static OctagonSurface instance;
    public Transform auv;
    public Transform[] octagonCylinders;
    public int pointsAvailable;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        this.enabled = false;
    }

    void Update()
    {
        Vector3 auvPosition = auv.position;

        // Not close to the surface.
        if (auvPosition.y <= -0.3)
        {
            return;
        }

        auvPosition.y = 0;

        if (IsObjectInsidePolygon(auvPosition))
        {
            PointsManager.instance.AddPoint(pointsAvailable, "Octagon");
            MessageBox.instance.AddMessage(string.Format("Octagon Surface +{0}pts", pointsAvailable));
            StopScript();
        }
    }

    bool IsObjectInsidePolygon(Vector3 auvPosition)
    {
        bool isInside = true;
        for (int i = 0; i < octagonCylinders.Length / 2; i++)
        {
            Vector3 cylinderA = octagonCylinders[i * 2].position;
            Vector3 cylinderB = octagonCylinders[i * 2 + 1].position;
            cylinderA.y = 0;
            cylinderB.y = 0;

            Vector3 cylinderA_auv = auvPosition - cylinderA;
            Vector3 cylinderA_cylinderB = cylinderB - cylinderA;
            float distSqr = cylinderA_cylinderB.sqrMagnitude;
            float d = Vector3.Dot(cylinderA_auv, cylinderA_cylinderB) / distSqr;
            isInside = isInside && d > 0 && d < 1;
        }

        return isInside;
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