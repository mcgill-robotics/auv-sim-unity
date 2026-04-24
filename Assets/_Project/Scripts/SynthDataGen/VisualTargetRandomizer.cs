using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

/// <summary>
/// Randomizes materials and labels on 1 or 2 quads (e.g., Gate targets or Task Boards) every frame.
/// Pairs each material with a specific label to ensure ground truth matches the visual.
/// </summary>
[Serializable]
[AddRandomizerMenu("RoboSub/Visual Target Randomizer")]
public class VisualTargetRandomizer : Randomizer
{
    #region Private Fields
    // Cache to store configs for each possible target, to avoid expensive GetChild calls every iteration. Key is the target Transform, value is a cache of its config GameObjects (child objects).
    private Dictionary<Transform, GameObjectOneWayCache> _caches = new Dictionary<Transform, GameObjectOneWayCache>();
    private HashSet<Transform> _preparedTargets = new HashSet<Transform>();
    // random state must be based on random state of FixedLengthScenario owned by BatchRunner, to ensure consistent reproducibility across all randomizers in the scenario
    private Unity.Mathematics.Random RandomState;

    #endregion

    #region Randomizer Lifecycle
    protected override void OnIterationStart()
    {
        // SamplerState is statically set by FixedLengthScenario through reflection, so we can rely on it to set the seed consistently
        RandomState = new Unity.Mathematics.Random(SamplerState.NextRandomState());

        // Find all tags in the scene and randomize their materials/labels
        var tags = tagManager.Query<VisualTargetRandomizerTag>();
        foreach (VisualTargetRandomizerTag tag in tags)
        {
            tag.InitializeConfigContainers();
            Transform[] targets = tag.targets;
            if (targets == null)
            {
                continue;
            }

            int targetCount = tag.GetTargetCount();
            int configCount = tag.GetConfigCount();

            if (targetCount == 0 || configCount == 0)
            {
                continue; // Nothing to randomize
            }

            // Get shuffled config indices to ensure different configs for each target
            int[] shuffledIndices = DataSynthRandom.GetShuffledIndices(configCount, RandomState);
            int validTargetOrdinal = 0;

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                Transform target = targets[targetIndex];
                if (target == null)
                {
                    continue;
                }

                // Wrap around if there are fewer configs than targets
                GameObject prefabToSpawn = tag.configs[shuffledIndices[validTargetOrdinal % configCount]];

                // Lazy initialize cache for this target if it doesn't exist yet
                if (!_caches.TryGetValue(target, out var cache))
                {
                    if (_preparedTargets.Add(target))
                    {
                        tag.ClearConfigContainerAt(targetIndex);
                    }

                    // Cache is parented to the container target so that objects are organized in the hierarchy and automatically cleaned up if target is destroyed
                    cache = new GameObjectOneWayCache(tag.ConfigContainers[targetIndex].transform, tag.configs, this);
                    _caches[target] = cache;
                }

                // initialize newly selected config for this target
                GameObject activeVariant = cache.GetOrInstantiate(prefabToSpawn);

                // Configure the spawned config's transform and labeling based on the tag's setup
                tag.ConfigureSpawnedObject(activeVariant);
                validTargetOrdinal++;
            }

        }
    }

    protected override void OnIterationEnd()
    {
        foreach (var cache in _caches.Values)
        {
            cache.ResetAllObjects();
        }
    }
    #endregion
}
