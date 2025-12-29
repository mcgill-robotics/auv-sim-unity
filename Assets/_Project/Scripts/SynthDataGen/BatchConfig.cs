using UnityEngine;

/// <summary>
/// ScriptableObject storing configuration values for one synthetic data batch.
/// Create assets via: Assets > Create > RoboSub > Batch Config
/// </summary>
[CreateAssetMenu(fileName = "BatchConfig", menuName = "RoboSub/Batch Config")]
public class BatchConfig : ScriptableObject
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
    
    [Tooltip("Separation distance between spawned objects")]
    public float separationDistance = 2.5f;
    
    #endregion
    
    #region Underwater Environment
    
    [Header("Underwater Environment")]
    [Tooltip("Minimum absorption distance (low = murky)")]
    public float absorptionDistanceMin = 4f;
    
    [Tooltip("Maximum absorption distance (high = clear)")]
    public float absorptionDistanceMax = 20f;
    
    [Tooltip("Minimum film grain intensity")]
    [Range(0f, 1f)]
    public float filmGrainMin = 0f;
    
    [Tooltip("Maximum film grain intensity")]
    [Range(0f, 1f)]
    public float filmGrainMax = 1.0f;
    
    #endregion
    
    #region Camera Settings
    
    [Header("Camera Settings")]
    [Tooltip("Whether camera should aim at spawned objects")]
    public bool lookAtSpawnedObject = true;
    
    [Tooltip("Minimum distance from camera to target")]
    public float cameraMinDistance = 2f;
    
    [Tooltip("Maximum distance from camera to target")]
    public float cameraMaxDistance = 8f;
    
    [Tooltip("Minimum camera pitch (X rotation)")]
    public float pitchMin = 10f;
    
    [Tooltip("Maximum camera pitch (X rotation)")]
    public float pitchMax = 60f;
    
    #endregion
}
