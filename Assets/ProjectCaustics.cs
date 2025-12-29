using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class ProjectCaustics : MonoBehaviour
{
    public WaterSurface waterSurface;

    void Update()
    {
        this.GetComponent<DecalProjector>().material.SetTexture("_Base_Color", waterSurface.GetCausticsBuffer(out float regionSize));
    }
}
