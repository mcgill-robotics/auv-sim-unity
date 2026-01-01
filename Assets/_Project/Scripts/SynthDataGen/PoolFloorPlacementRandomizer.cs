using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

/// <summary>
/// Configuration for a single prefab with its spawn height.
/// </summary>
[Serializable]
public class PrefabPlacementConfig
{
    [Tooltip("The prefab to spawn")]
    public GameObject prefab;
    
    [Tooltip("Y height for this prefab (e.g., -2.1 for floor objects, 0 for hanging objects)")]
    public float spawnHeight = -2.1f;
}

/// <summary>
/// Places objects on a horizontal floor (XZ plane) using Poisson Disk sampling.
/// Supports per-prefab height configuration for objects with different pivot points.
/// </summary>
/// <remarks>
/// - Objects with bottom pivot (Lane Marker, Pipes, Bin, Table): use floorHeight (-2.1)
/// - Objects with top pivot (Gate, Octagon): use surface height (0)
/// </remarks>
[Serializable]
[AddRandomizerMenu("RoboSub/Pool Floor Placement Randomizer")]
public class PoolFloorPlacementRandomizer : Randomizer
{
    #region Placement Settings
    
    [Header("Placement Area")]
    [Tooltip("The size of the placement area (X = width, Y = depth/length on Z axis)")]
    public Vector2 placementArea = new Vector2(20f, 40f);
    
    [Tooltip("The minimum distance between object centers")]
    public float separationDistance = 2.5f;
    
    [Tooltip("Maximum number of objects to spawn per iteration (0 = unlimited by Poisson)")]
    public int maxObjectCount = 0;
    
    [Tooltip("If false, no objects will be spawned, but the randomizer remains active to ensure cleanup.")]
    public bool shouldSpawn = true;
    
    #endregion
    
    #region Prefab Configuration
    
    [Header("Prefabs with Height")]
    [Tooltip("Configure each prefab with its spawn height")]
    public List<PrefabPlacementConfig> prefabConfigs = new List<PrefabPlacementConfig>();
    
    #endregion
    
    #region Private Fields
    
    private GameObject _container;
    private GameObjectOneWayCache _gameObjectCache;
    
    #endregion
    
    #region Randomizer Lifecycle
    
    protected override void OnAwake()
    {
        _container = new GameObject("Foreground Objects");
        _container.transform.parent = scenario.transform;
        
        if (prefabConfigs == null || prefabConfigs.Count == 0)
        {
            Debug.LogWarning("[PoolFloorPlacementRandomizer] No prefab configs assigned!");
            return;
        }
        
        // Filter out null prefabs
        var validConfigs = prefabConfigs.Where(c => c != null && c.prefab != null).ToList();
        if (validConfigs.Count == 0)
        {
            Debug.LogWarning("[PoolFloorPlacementRandomizer] No valid prefabs in config!");
            return;
        }
        
        // Build prefab array for cache
        var prefabArray = validConfigs.Select(c => c.prefab).ToArray();
        _gameObjectCache = new GameObjectOneWayCache(_container.transform, prefabArray, this);
    }

    protected override void OnIterationStart()
    {
        if (!shouldSpawn || _gameObjectCache == null || prefabConfigs == null || prefabConfigs.Count == 0)
        {
            return;
        }
        
        // Generate Poisson Disk samples
        var seed = SamplerState.NextRandomState();
        using (var nativeSamples = PoissonDiskSampling.GenerateSamples(
            placementArea.x, 
            placementArea.y, 
            separationDistance, 
            seed
        ))
        {
            // Shuffle prefabs to randomize which object type appears in the sequence
            var prefabsToSpawn = prefabConfigs
                .Where(c => c != null && c.prefab != null)
                .ToList();

            for (int i = prefabsToSpawn.Count - 1; i > 0; i--)
            {
                int k = UnityEngine.Random.Range(0, i + 1);
                var temp = prefabsToSpawn[k];
                prefabsToSpawn[k] = prefabsToSpawn[i];
                prefabsToSpawn[i] = temp;
            }
            
            // Center offset so placement area is centered at origin
            var centerOffset = new Vector3(placementArea.x * 0.5f, 0f, placementArea.y * 0.5f);
            
            // Place unique objects 1-to-1 until we run out of samples or prefabs
            int spawnCount = Mathf.Min(nativeSamples.Length, prefabsToSpawn.Count);
            if (maxObjectCount > 0) spawnCount = Mathf.Min(spawnCount, maxObjectCount);

            for (int i = 0; i < spawnCount; i++)
            {
                var sample = nativeSamples[i];
                var config = prefabsToSpawn[i];
                
                var instance = _gameObjectCache.GetOrInstantiate(config.prefab);
                
                // Place on XZ plane with per-prefab height
                instance.transform.position = new Vector3(
                    sample.x - centerOffset.x,
                    config.spawnHeight,
                    sample.y - centerOffset.z
                );
            }
        }
    }

    protected override void OnIterationEnd()
    {
        // Always reset, even if disabled, to ensure objects from previous batch are cleaned up
        _gameObjectCache?.ResetAllObjects();
    }
    
    /// <summary>
    /// Explicitly clear all spawned objects.
    /// </summary>
    public void ClearObjects()
    {
        _gameObjectCache?.ResetAllObjects();
    }
    
    #endregion
}

