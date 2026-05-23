using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Utilities;

/// <summary>
/// Randomizes object placement on tables at each iteration of the scenario.
/// Triggers the TablePlacementRandomizerTag on each table to spawn objects with Poisson Disk sampling to ensure variety while avoiding object overlap.
/// Objects are cleaned up at the end of each iteration to ensure a fresh start for the next. 
/// CURRENTLY NO CACHE USED, TODO
/// </summary>
[Serializable]
[AddRandomizerMenu("RoboSub/Table Placement Randomizer")]
public class TablePlacementRandomizer : Randomizer
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

        // Find all tags in the scene and randomize their objects
        // Technically there is only one table needed ever since down cam
        // is hard to have two tables in view at once
        var tags = tagManager.Query<TablePlacementRandomizerTag>();
        Debug.Log($"[TablePlacementRandomizer] Found {tags} tags to randomize.");
        foreach (TablePlacementRandomizerTag tag in tags)
        {
          tag.SpawnObjects();
        }
    }

    protected override void OnIterationEnd()
    {
      // Find all tags in the scene and clear their objects to ensure clean slate for next iteration
      var tags = tagManager.Query<TablePlacementRandomizerTag>();
      foreach (TablePlacementRandomizerTag tag in tags)
      {
        
          tag.ClearObjects();
      }
    }
    #endregion
}
