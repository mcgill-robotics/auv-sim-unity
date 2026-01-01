using System;
using UnityEngine;

/// <summary>
/// Serializable configuration for one synthetic data batch.
/// These are configured directly in the BatchRunner inspector.
/// </summary>
[Serializable]
public class BatchConfig : ISerializationCallbackReceiver
{
    #region Batch Info
    
    [Header("Batch Info")]
    [Tooltip("Name for this batch (e.g., 'HighVisibility', 'Murky')")]
    public string batchName = "NewBatch";
    
    [Tooltip("Number of iterations to run for this batch")]
    public int iterations = 1000;
    
    #endregion
    
    #region Object Placement
    
    [Header("Object Placement")]
    [Tooltip("If false, no objects are spawned (Background Only batch)")]
    public bool spawnObjects = true;
    
    #endregion
    
    #region Underwater Environment
    
    [Header("Water Visibility")]
    [Tooltip("Min/Max absorption distance (low = murky)")]
    public Vector2 absorptionDistanceRange = new Vector2(4f, 25f);
    
    [Header("Water Color (Physical)")]
    [Tooltip("Physical water scattering color ranges")]
    public Vector2 waterScatterRedRange = new Vector2(0.0f, 0.4f);
    public Vector2 waterScatterGreenRange = new Vector2(0.2f, 0.8f);
    public Vector2 waterScatterBlueRange = new Vector2(0.3f, 1.0f);

    [Header("Color Filter (Post-Process)")]
    [Tooltip("Post-processing tint ranges")]
    public Vector2 filterRedRange = new Vector2(0.3f, 0.8f);
    public Vector2 filterGreenRange = new Vector2(0.5f, 1.0f);
    public Vector2 filterBlueRange = new Vector2(0.6f, 1.0f);

    [Header("Color Grading")]
    public Vector2 postExposureRange = new Vector2(-1f, 1f);
    public Vector2 contrastRange = new Vector2(-30f, 25f);
    public Vector2 saturationRange = new Vector2(-80f, 0f);

    [Header("Sensor Noise")]
    [Range(0f, 1f)]
    public float filmGrainMax = 1.0f;
    
    #endregion
    
    #region Camera Settings
    
    [Header("Camera Settings")]
    [Tooltip("Whether camera should aim at spawned objects")]
    public bool lookAtSpawnedObject = true;
    
    [Tooltip("Placement distance from target")]
    public Vector2 cameraDistanceRange = new Vector2(2f, 8f);
    
    [Tooltip("Camera pitch (X rotation) range. Negative = Up, Positive = Down.")]
    public Vector2 pitchRange = new Vector2(-10f, 30f);

    [Tooltip("Yaw range (only used when LookAt is false)")]
    public Vector2 randomYawRange = new Vector2(0f, 360f);

    [Tooltip("Roll (Z rotation) range (AUV wobble)")]
    public Vector2 rollRange = new Vector2(-10f, 10f);

    [Tooltip("Yaw jitter around look-at direction (deg)")]
    public float yawJitter = 20f;
    
    #endregion

    /// <summary>
    /// Explicit constructor for initialization.
    /// </summary>
    public BatchConfig()
    {
        SetDefaults();
    }

    /// <summary>
    /// Force setup of default values.
    /// </summary>
    public void SetDefaults()
    {
        batchName = "NewBatch";
        iterations = 1000;
        spawnObjects = true;
        
        absorptionDistanceRange = new Vector2(4f, 25f);
        
        waterScatterRedRange = new Vector2(0.0f, 0.4f);
        waterScatterGreenRange = new Vector2(0.2f, 0.8f);
        waterScatterBlueRange = new Vector2(0.3f, 1.0f);

        filterRedRange = new Vector2(0.3f, 0.8f);
        filterGreenRange = new Vector2(0.5f, 1.0f);
        filterBlueRange = new Vector2(0.6f, 1.0f);
        
        postExposureRange = new Vector2(-1f, 1f);
        contrastRange = new Vector2(-30f, 25f);
        saturationRange = new Vector2(-80f, 0f);
        filmGrainMax = 1.0f;
        
        lookAtSpawnedObject = true;
        cameraDistanceRange = new Vector2(2f, 8f);
        pitchRange = new Vector2(-10f, 30f);
        randomYawRange = new Vector2(0f, 360f);
        rollRange = new Vector2(-10f, 10f);
        yawJitter = 20f;
    }


    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        // If iterations is 0, it likely means this is a newly created list entry that Unity zeroed out.
        // We apply defaults in this case.
        if (iterations == 0)
        {
            SetDefaults();
        }
    }

}
