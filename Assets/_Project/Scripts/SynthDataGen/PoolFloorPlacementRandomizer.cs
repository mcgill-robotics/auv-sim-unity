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
    
    [Tooltip("Probability weight for spawning this prefab (higher = more frequent)")]
    [Range(0.1f, 10f)]
    public float weight = 1f;
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
    
    #endregion
    
    #region Prefab Configuration
    
    [Header("Prefabs with Height")]
    [Tooltip("Configure each prefab with its spawn height")]
    public List<PrefabPlacementConfig> prefabConfigs = new List<PrefabPlacementConfig>();
    
    #endregion
    
    #region Private Fields
    
    private GameObject _container;
    private GameObjectOneWayCache _gameObjectCache;
    private Dictionary<int, float> _prefabHeights;  // Maps prefab instance ID to height
    private float _totalWeight;
    
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
        
        // Build prefab array and height lookup
        var prefabArray = validConfigs.Select(c => c.prefab).ToArray();
        _prefabHeights = new Dictionary<int, float>();
        _totalWeight = 0f;
        
        foreach (var config in validConfigs)
        {
            _prefabHeights[config.prefab.GetInstanceID()] = config.spawnHeight;
            _totalWeight += config.weight;
        }
        
        _gameObjectCache = new GameObjectOneWayCache(_container.transform, prefabArray, this);
    }

    protected override void OnIterationStart()
    {
        if (_gameObjectCache == null || prefabConfigs == null || prefabConfigs.Count == 0)
        {
            return;
        }
        
        // Generate Poisson Disk samples
        var seed = SamplerState.NextRandomState();
        var samples = PoissonDiskSampling.GenerateSamples(
            placementArea.x, 
            placementArea.y, 
            separationDistance, 
            seed
        );
        
        // Center offset so placement area is centered at origin
        var centerOffset = new Vector3(placementArea.x * 0.5f, 0f, placementArea.y * 0.5f);
        
        int objectCount = 0;
        foreach (var sample in samples)
        {
            // Check max count limit
            if (maxObjectCount > 0 && objectCount >= maxObjectCount)
                break;
            
            // Sample a weighted random prefab
            var selectedConfig = SampleWeightedPrefab();
            if (selectedConfig == null || selectedConfig.prefab == null)
                continue;
            
            var instance = _gameObjectCache.GetOrInstantiate(selectedConfig.prefab);
            
            // Place on XZ plane with per-prefab height
            instance.transform.position = new Vector3(
                sample.x - centerOffset.x,
                selectedConfig.spawnHeight,
                sample.y - centerOffset.z
            );
            
            objectCount++;
        }
        
        samples.Dispose();
    }

    protected override void OnIterationEnd()
    {
        _gameObjectCache?.ResetAllObjects();
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Samples a prefab config based on weights.
    /// </summary>
    private PrefabPlacementConfig SampleWeightedPrefab()
    {
        if (_totalWeight <= 0)
            return prefabConfigs.FirstOrDefault(c => c?.prefab != null);
        
        float randomValue = UnityEngine.Random.Range(0f, _totalWeight);
        float cumulative = 0f;
        
        foreach (var config in prefabConfigs)
        {
            if (config == null || config.prefab == null)
                continue;
                
            cumulative += config.weight;
            if (randomValue <= cumulative)
                return config;
        }
        
        return prefabConfigs.FirstOrDefault(c => c?.prefab != null);
    }
    
    #endregion
}

