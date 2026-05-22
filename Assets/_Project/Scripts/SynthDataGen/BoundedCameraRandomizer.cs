using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

/// <summary>
/// Randomizes camera position and rotation within defined pool bounds.
/// Automatically targets spawned foreground objects for data generation.
/// </summary>
/// <remarks>
/// Must be ordered AFTER ForegroundObjectPlacementRandomizer in the randomizer stack
/// so that objects exist when this randomizer runs.
/// </remarks>
[AddRandomizerMenu("RoboSub/Bounded Camera Randomizer")]
public class BoundedCameraRandomizer : Randomizer
{
    #region References

    [Header("Camera Reference")]
    [Tooltip("The camera transform to randomize (typically Main Camera with PerceptionCamera)")]
    public Transform cameraTransform;

    #endregion

    #region Pool Bounds

    [Header("Pool Safe Zone (World Coordinates)")]
    [Tooltip("Lower corner of the valid camera volume")]
    public Vector3 poolMinBounds = new Vector3(-12f, -2f, -24f);

    [Tooltip("Upper corner of the valid camera volume")]
    public Vector3 poolMaxBounds = new Vector3(12f, -0.16f, 24f);

    #endregion

    #region Distance Settings

    [Header("Distance from Target")]
    [Tooltip("If true, spawns camera within distance range of target object")]
    public bool constrainDistanceToTarget = true;

    [Tooltip("Minimum distance from camera to target object (meters)")]
    public float minDistance = 2f;

    [Tooltip("Maximum distance from camera to target object (meters)")]
    public float maxDistance = 8f;

    #endregion

    #region Look-At Settings

    [Header("Camera Aiming")]
    [Tooltip("If true, camera aims at a random spawned object. If false, uses fully random yaw.")]
    public bool lookAtSpawnedObject = true;

    [Header("Down Cam")]
    [Tooltip("If true, camera is spawned above target looking down. Otherwise, camera is spawned around target at random yaw.")]
    public bool downCam = false;

    [Tooltip("Fallback target if no objects are spawned (pool floor center)")]
    public Vector3 fallbackTarget = new Vector3(0f, -2.1f, 0f);

    [Tooltip("Yaw jitter around look-at direction (degrees). ±30 = 60° cone toward target.")]
    public FloatParameter yawJitter = new FloatParameter { value = new UniformSampler(-30f, 30f) };

    #endregion

    #region Rotation Parameters

    [Header("Camera Rotation")]
    [Tooltip("Pitch: X-axis rotation (looking up/down). 10-60 = mostly looking down.")]
    public FloatParameter pitch = new FloatParameter { value = new UniformSampler(10f, 60f) };

    [Tooltip("Yaw: Y-axis rotation. Only used when lookAtSpawnedObject is false.")]
    public FloatParameter yaw = new FloatParameter { value = new UniformSampler(0f, 360f) };

    [Tooltip("Roll: Z-axis rotation (AUV instability). -5 to 5 = slight wobble.")]
    public FloatParameter roll = new FloatParameter { value = new UniformSampler(-5f, 5f) };

    #endregion

    #region Private Fields

    private Transform _foregroundContainer;
    private const int MaxPositionAttempts = 10;
    // random state must be based on random state of FixedLengthScenario owned by BatchRunner, to ensure consistent reproducibility across all randomizers in the scenario
    private Unity.Mathematics.Random RandomState;

    #endregion

    #region Randomizer Lifecycle

    protected override void OnScenarioStart()
    {
        // Find the container created by ForegroundObjectPlacementRandomizer
        var containerObj = GameObject.Find("Foreground Objects");
        if (containerObj != null)
        {
            _foregroundContainer = containerObj.transform;
        }
    }

    protected override void OnIterationStart()
    {
        // SamplerState is statically set by FixedLengthScenario through reflection, so we can rely on it to set the seed consistently
        RandomState = new Unity.Mathematics.Random(SamplerState.NextRandomState());

        if (!enabled || cameraTransform == null) return;

        // 1. Pick target object first
        Vector3 targetPosition = GetRandomSpawnedObjectPosition();

        // 2. Generate camera position
        Vector3 cameraPosition;

        if (constrainDistanceToTarget && lookAtSpawnedObject)
        {
            if (!downCam)
            {
                // Spawn within distance range of target, respecting pool bounds
                cameraPosition = GeneratePositionNearTarget(targetPosition);
            }
            else
            {
                // Spawn within distance range of target above it, respecting pool bounds
                cameraPosition = GeneratePositionAboveTarget(targetPosition);
            }
        }
        else
        {
            // Fully random position within pool bounds
            cameraPosition = GenerateRandomPoolPosition();
        }

        cameraTransform.position = cameraPosition;

        // 3. Calculate Yaw
        float finalYaw;

        if (lookAtSpawnedObject)
        {
            // Calculate direction to target and extract yaw angle
            Vector3 directionToTarget = targetPosition - cameraPosition;
            directionToTarget.y = 0; // Project onto horizontal plane

            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                float baseYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
                finalYaw = baseYaw + yawJitter.Sample();
            }
            else
            {
                finalYaw = yaw.Sample();
            }
        }
        else
        {
            // Fully random yaw (for background-only batches)
            finalYaw = yaw.Sample();
        }

        // 4. Apply Rotation
        cameraTransform.rotation = Quaternion.Euler(pitch.Sample(), finalYaw, roll.Sample());
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a random position within pool bounds.
    /// </summary>
    private Vector3 GenerateRandomPoolPosition()
    {
        return new Vector3(
            RandomState.NextFloat(poolMinBounds.x, poolMaxBounds.x),
            RandomState.NextFloat(poolMinBounds.y, poolMaxBounds.y),
            RandomState.NextFloat(poolMinBounds.z, poolMaxBounds.z)
        );
    }

    /// <summary>
    /// Generates a position within distance range of target, clamped to pool bounds.
    /// </summary>
    private Vector3 GeneratePositionNearTarget(Vector3 targetPosition)
    {
        for (int i = 0; i < MaxPositionAttempts; i++)
        {
            // Random distance within range
            float distance = RandomState.NextFloat(minDistance, maxDistance);

            // Random direction (horizontal)
            float angle = RandomState.NextFloat(0f, 360f) * Mathf.Deg2Rad;

            // Random height within pool bounds
            float height = RandomState.NextFloat(poolMinBounds.y, poolMaxBounds.y);

            // Calculate position offset from target
            Vector3 offset = new Vector3(
                Mathf.Sin(angle) * distance,
                0f, // Height is set absolutely, not as offset
                Mathf.Cos(angle) * distance
            );

            Vector3 candidatePosition = new Vector3(
                targetPosition.x + offset.x,
                height, // Use random height directly
                targetPosition.z + offset.z
            );

            // Clamp to pool bounds
            candidatePosition.x = Mathf.Clamp(candidatePosition.x, poolMinBounds.x, poolMaxBounds.x);
            candidatePosition.y = Mathf.Clamp(candidatePosition.y, poolMinBounds.y, poolMaxBounds.y);
            candidatePosition.z = Mathf.Clamp(candidatePosition.z, poolMinBounds.z, poolMaxBounds.z);

            // Verify distance is still acceptable after clamping
            float actualDistance = Vector3.Distance(
                new Vector3(candidatePosition.x, 0, candidatePosition.z),
                new Vector3(targetPosition.x, 0, targetPosition.z)
            );

            if (actualDistance >= minDistance * 0.5f) // Allow some tolerance after clamping
            {
                return candidatePosition;
            }
        }

        // Fallback: return random pool position
        return GenerateRandomPoolPosition();
    }

    /// <summary>
    /// Generates a position above target within distance range of target, clamped to pool bounds.
    /// </summary>
    private Vector3 GeneratePositionAboveTarget(Vector3 targetPosition)
    {
        for (int i = 0; i < MaxPositionAttempts; i++)
        {
            // Random distance within range
            float distance = RandomState.NextFloat(minDistance, maxDistance);

            // Random direction (horizontal)
            float angle_theta = RandomState.NextFloat(0f, 360f) * Mathf.Deg2Rad;

            // Random direction relative to height (using spherical coordinates)
            float angle_phi = RandomState.NextFloat(-30f, 30f) * Mathf.Deg2Rad;

            // Calculate position offset from target
            Vector3 offset = new Vector3(
                distance * Mathf.Sin(angle_phi) * Mathf.Cos(angle_theta),
                distance * Mathf.Cos(angle_phi),
                distance * Mathf.Sin(angle_phi) * Mathf.Sin(angle_theta)
            );

            Vector3 candidatePosition = new Vector3(
                targetPosition.x + offset.x,
                targetPosition.y + offset.y,
                targetPosition.z + offset.z
            );

            // Clamp to pool bounds
            candidatePosition.x = Mathf.Clamp(candidatePosition.x, poolMinBounds.x, poolMaxBounds.x);
            candidatePosition.y = Mathf.Clamp(candidatePosition.y, poolMinBounds.y, poolMaxBounds.y);
            candidatePosition.z = Mathf.Clamp(candidatePosition.z, poolMinBounds.z, poolMaxBounds.z);

            // Verify distance is still acceptable after clamping
            float actualDistance = Vector3.Distance(
                new Vector3(candidatePosition.x, candidatePosition.y, candidatePosition.z),
                new Vector3(targetPosition.x, targetPosition.y, targetPosition.z)
            );

            if (actualDistance >= minDistance * 0.5f) // Allow some tolerance after clamping
            {
                return candidatePosition;
            }
        }

        // Fallback: return random pool position
        return GenerateRandomPoolPosition();
    }

    /// <summary>
    /// Gets the position of a random active spawned object, or fallback if none exist.
    /// </summary>
    private Vector3 GetRandomSpawnedObjectPosition()
    {
        if (_foregroundContainer == null)
        {
            Debug.LogWarning("[BoundedCameraRandomizer] Foreground container is null, using fallback target");
            return fallbackTarget;
        }

        if (_foregroundContainer.childCount == 0)
        {
            Debug.LogWarning("[BoundedCameraRandomizer] Foreground container has no children, using fallback target");
            return fallbackTarget;
        }

        // Collect active children that are actually in the scene (not at cache reset position)
        var activeObjects = new List<Transform>();
        foreach (Transform child in _foregroundContainer)
        {
            // Filter out pooled objects at X=10000 (GameObjectOneWayCache reset position)
            if (child.gameObject.activeInHierarchy && child.position.x < 9999f)
            {
                activeObjects.Add(child);
            }
        }

        if (activeObjects.Count == 0)
        {
            // Debug.LogWarning("[BoundedCameraRandomizer] No valid spawned objects found (all at cache position), using fallback target");
            return fallbackTarget;
        }

        // Pick a random active object
        int randomIndex = RandomState.NextInt(0, activeObjects.Count);
        return activeObjects[randomIndex].position;
    }

    #endregion
}



