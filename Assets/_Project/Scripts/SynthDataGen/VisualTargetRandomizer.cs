using System;
using System.Collections.Generic;
using System.Linq;
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
            int targetCount = tag.GetTargetCount();
            int configCount = tag.GetConfigCount();

            if (targetCount == 0 || configCount == 0)
            {
                continue; // Nothing to randomize
            }

            // Get shuffled config indices to ensure different configs for each target
            int[] shuffledIndices = GetShuffledIndices(configCount);

            for (int i = 0; i < targetCount; i++)
            {
                // Wrap around if fewer configs than targets
                tag.RandomizeMaterials(i, shuffledIndices[i % configCount]);
            }

        }
    }
    #endregion

    #region Helpers
    private int[] GetShuffledIndices(int count)
    {
        // Shuffle a copy of the config indices, then assign in order
        // Fisher-Yates shuffle guarantees no two targets get the same config
        int[] indices = Enumerable.Range(0, count).ToArray();
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = RandomState.NextInt(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
            // index j is swapped to position i, and so after shuffle we do not touch config at index i again (due to i-- and upper bound of NextInt), ensuring it is not assigned to another target 
        }
        return indices;
    }
    #endregion
}