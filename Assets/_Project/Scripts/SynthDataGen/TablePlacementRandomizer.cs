using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Utilities;

/// <summary>
/// Configuration for a single prefab with its relative
/// spawn height to a parent object.
/// </summary>
[Serializable]
public class RelativePrefabPlacementConfig
{
  [Tooltip("Prefab to be spawned.")]
  public GameObject prefab;

  [Tooltip("Relative Y height at which to spawn the prefab, in meters.")]
  public float relativeHeight;
}

/// <summary>
/// Places objects on top of an object, such as a table, with a randomized distance given user defined bounds. Utilizes a poisson Distribution to ensure objects are not placed too close together.
/// </summary>
public class TablePlacementRandomizer : MonoBehaviour
{
  #region Placement Settings
  [Header("Generation seed")]
  [Tooltip("Seed for randomization. Set to 0 for a random seed each time.")]
  public uint seed;

  [Header("Reference Transform")]
  [Tooltip("The transform on which to place objects. Objects will be placed at a height relative to this transform.")]
  public Transform referenceTransform;

  [Header("Placement Area")]
  [Tooltip("The area around the reference transform within which to place objects.")]
  public Vector2 placementArea = new Vector2(1f, 1f);
  
  [Tooltip("The separation between placed objects, in meters.")]
  public float separationDistance = 0.4f;

  [Tooltip("The max number of objects to place. There might be less than this number of objects placed if the random distribution does not allow for more objects to be placed without violating the minimum distance requirement.")]
  public int maxObjectCount = 5;

  [Tooltip("Yaw range of placed objects.")]
  public Vector2 yawRange = new Vector2(0f, 360f);
  #endregion 

  #region Prefab Configuration
  [Header("Prefab Configuration")]
  [Tooltip("List of prefabs to spawn with their relative height to the reference transform.")]
  public List<RelativePrefabPlacementConfig> prefabConfigs;
  #endregion

  #region Randomizer Lifecycle

  protected void Awake()
  {
    SpawnObjects();
  }

  /// <summary>
  /// Spawn objects
  /// </summary>
  public void SpawnObjects() 
  {
    if (referenceTransform == null)
    {
      Debug.LogError("Reference Transform is not set. Cannot spawn objects.");
      return;
    }

    if (prefabConfigs == null || prefabConfigs.Count == 0)
    {
      Debug.LogError("No prefab configurations provided. Cannot spawn objects.");
      return;
    }

    var validConfigs = prefabConfigs.Where(config => config.prefab != null).ToList();

    if (validConfigs.Count == 0)
    {
      Debug.LogError("All prefab configurations are invalid. Cannot spawn objects.");
      return;
    }

    // Initialize random state
    Unity.Mathematics.Random randomState = new Unity.Mathematics.Random(seed);

    // Generate positions using Poisson Disk Sampling
    using (var nativeSamples = PoissonDiskSampling.GenerateSamples(
      placementArea.x,
      placementArea.y,
      separationDistance,
      seed
    ))
    {
      // Center offset so placement area is cenetered at origin
      Vector3 centerOffset = new Vector3(placementArea.x * 0.5f, 0f, placementArea.y * 0.5f);

      // Determine whether to use the object spawn count limit or the number of valid samples as the upper bound for spawning objects
      int spawnCount = 0;
      if (maxObjectCount > 0)
      {
        spawnCount = Math.Min(maxObjectCount, nativeSamples.Length);
      }

      // Spawn objects at generated positions
      for (int i = 0; i< spawnCount; i++)
      {
        var sample = nativeSamples[i];

        // Go through all prefabs at least once to guarantee at least one of 
        // each is spawned (if spawnCount permits).
        // Once we've spawned all of them at least once, pick randomly
        // for the remaining duplicates.
        var config = i < prefabConfigs.Count 
          ? validConfigs[i] 
          : validConfigs[randomState.NextInt(0, validConfigs.Count)];

          // Place on XZ plane with with per prefab height
          Vector3 localPosition = new Vector3(
            sample.x - centerOffset.x, 
            config.relativeHeight, 
            sample.y - centerOffset.z
          );

          // Convert to world position from the referenceTransform
          Vector3 worldPosition = referenceTransform.TransformPoint
          (localPosition);

          // Randomize yaw within specified range
          float yaw = randomState.NextFloat(yawRange.x, yawRange.y);
          Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

          // Spawn the object
          GameObject instance = Instantiate(config.prefab, worldPosition, rotation, transform);

          // Set the layer of the spawned object and all its children to props so that the camera can pick up on it
          SetLayerRecursively(instance, LayerMask.NameToLayer("Props"));
      }
    }
  }

  // Utility function to set the layer of a game object and all its children
  private void SetLayerRecursively(GameObject obj, int layer)
  {
    obj.layer = layer;
    
    foreach (Transform child in obj.transform)
    {
      SetLayerRecursively(child.gameObject, layer);
    }
  }
  #endregion
} 