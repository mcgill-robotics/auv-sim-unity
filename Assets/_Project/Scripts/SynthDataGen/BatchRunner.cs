using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;

// Aliases
using PerceptionFloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;
using ColorRgbParameter = UnityEngine.Perception.Randomization.Parameters.ColorRgbParameter;

public class BatchRunner : MonoBehaviour
{
    [System.Serializable]
    public struct BatchRuntimeInfo
    {
        public int startIteration;
        public int endIteration;
        public BatchConfig config;
    }

    #region Settings
    [Header("Batch Configurations")]
    public List<BatchConfig> batchConfigs = new List<BatchConfig>();

    [Header("Run Mode")]
    public bool testModeEnabled = false;
    [Tooltip("Index of the batch to run in Test Mode.")]
    public int testBatchIndex = 0;
    #endregion

    #region Runtime State
    private FixedLengthScenario _scenario;
    private List<BatchRuntimeInfo> _schedule = new List<BatchRuntimeInfo>();
    private int _currentBatchIndex = -1;
    
    // Randomizers
    private PoolFloorPlacementRandomizer _placementRandomizer;
    private UnderwaterEnvironmentRandomizer _underwaterRandomizer;
    private BoundedCameraRandomizer _cameraRandomizer;
    #endregion

    private void Awake()
    {
        _scenario = GetComponent<FixedLengthScenario>();
        if (!_scenario) _scenario = FindObjectOfType<FixedLengthScenario>();

        if (!_scenario)
        {
            Debug.LogError("[BatchRunner] Critical: No FixedLengthScenario found!");
            enabled = false;
            return;
        }

        // Cache Randomizers
        _placementRandomizer = GetRandomizer<PoolFloorPlacementRandomizer>();
        _underwaterRandomizer = GetRandomizer<UnderwaterEnvironmentRandomizer>();
        _cameraRandomizer = GetRandomizer<BoundedCameraRandomizer>();
    }

    private void Start()
    {
        BuildSchedule();
        
        if (_schedule.Count > 0)
        {
            // Configure the Scenario to run the FULL length of all batches combined
            int totalIterations = _schedule[_schedule.Count - 1].endIteration;
            _scenario.constants.iterationCount = totalIterations;
            
            Debug.Log($"[BatchRunner] Simulation configured for {totalIterations} total iterations across {_schedule.Count} batches.");
            
            // Apply first batch immediately
            UpdateBatchConfig(0);
        }
    }

    private void Update()
    {
        // Monitor the current iteration to see if we need to switch batches
        int currentIter = _scenario.currentIteration;

        // Check if we have moved into a new batch's territory
        // We use the schedule to find which batch owns the current iteration
        int batchIndex = GetBatchIndexForIteration(currentIter);

        if (batchIndex != -1 && batchIndex != _currentBatchIndex)
        {
            UpdateBatchConfig(batchIndex);
        }
    }

    private void BuildSchedule()
    {
        _schedule.Clear();
        int currentIterCount = 0;

        if (testModeEnabled)
        {
            // Just run the single test batch
            if (testBatchIndex >= 0 && testBatchIndex < batchConfigs.Count)
            {
                var config = batchConfigs[testBatchIndex];
                _schedule.Add(new BatchRuntimeInfo
                {
                    startIteration = 0,
                    endIteration = config.iterations,
                    config = config
                });
            }
        }
        else
        {
            // Chain all batches
            foreach (var config in batchConfigs)
            {
                int start = currentIterCount;
                int end = start + config.iterations;
                
                _schedule.Add(new BatchRuntimeInfo
                {
                    startIteration = start,
                    endIteration = end,
                    config = config
                });

                currentIterCount = end;
            }
        }
    }

    private int GetBatchIndexForIteration(int iteration)
    {
        for (int i = 0; i < _schedule.Count; i++)
        {
            if (iteration >= _schedule[i].startIteration && iteration < _schedule[i].endIteration)
            {
                return i;
            }
        }
        return -1;
    }

    private void UpdateBatchConfig(int index)
    {
        _currentBatchIndex = index;
        var info = _schedule[index];
        var config = info.config;

        Debug.Log($"<color=cyan>[BatchRunner]</color> Switching to Batch {index + 1}/{_schedule.Count}: <b>{config.batchName}</b> (Iters {info.startIteration}-{info.endIteration})");

        // --- Apply Configs ---

        // 1. Placement (Toggle objects)
        if (_placementRandomizer != null)
        {
            _placementRandomizer.enabled = true; // Always stays enabled for cleanup
            _placementRandomizer.shouldSpawn = config.spawnObjects;
            
            if (!config.spawnObjects)
            {
                _placementRandomizer.ClearObjects();
            }
        }

        // 2. Underwater Environment (Visuals)
        if (_underwaterRandomizer != null)
        {
            _underwaterRandomizer.enabled = true;
            _underwaterRandomizer.absorptionDistance = new PerceptionFloatParameter { value = new UniformSampler(config.absorptionDistanceRange.x, config.absorptionDistanceRange.y) };
            _underwaterRandomizer.filmGrainIntensity = new PerceptionFloatParameter { value = new UniformSampler(0f, config.filmGrainMax) };
            _underwaterRandomizer.postExposure = new PerceptionFloatParameter { value = new UniformSampler(config.postExposureRange.x, config.postExposureRange.y) };
            _underwaterRandomizer.contrast = new PerceptionFloatParameter { value = new UniformSampler(config.contrastRange.x, config.contrastRange.y) };
            _underwaterRandomizer.saturation = new PerceptionFloatParameter { value = new UniformSampler(config.saturationRange.x, config.saturationRange.y) };

            _underwaterRandomizer.colorFilter = new ColorRgbParameter
            {
                red = new UniformSampler(config.filterRedRange.x, config.filterRedRange.y),
                green = new UniformSampler(config.filterGreenRange.x, config.filterGreenRange.y),
                blue = new UniformSampler(config.filterBlueRange.x, config.filterBlueRange.y),
                alpha = new ConstantSampler(1f)
            };

            _underwaterRandomizer.waterScatterColor = new ColorRgbParameter
            {
                red = new UniformSampler(config.waterScatterRedRange.x, config.waterScatterRedRange.y),
                green = new UniformSampler(config.waterScatterGreenRange.x, config.waterScatterGreenRange.y),
                blue = new UniformSampler(config.waterScatterBlueRange.x, config.waterScatterBlueRange.y),
                alpha = new ConstantSampler(1f)
            };
        }

        // 3. Camera (Movement)
        if (_cameraRandomizer != null)
        {
            _cameraRandomizer.enabled = true;
            _cameraRandomizer.lookAtSpawnedObject = config.lookAtSpawnedObject;
            _cameraRandomizer.minDistance = config.cameraDistanceRange.x;
            _cameraRandomizer.maxDistance = config.cameraDistanceRange.y;

            _cameraRandomizer.pitch = new PerceptionFloatParameter { value = new UniformSampler(config.pitchRange.x, config.pitchRange.y) };
            _cameraRandomizer.yaw = new PerceptionFloatParameter { value = new UniformSampler(config.randomYawRange.x, config.randomYawRange.y) };
            _cameraRandomizer.roll = new PerceptionFloatParameter { value = new UniformSampler(config.rollRange.x, config.rollRange.y) };
            _cameraRandomizer.yawJitter = new PerceptionFloatParameter { value = new UniformSampler(-config.yawJitter, config.yawJitter) };
        }
    }

    private T GetRandomizer<T>() where T : Randomizer
    {
        try
        {
            return _scenario.GetRandomizer<T>();
        }
        catch
        {
            Debug.LogWarning($"[BatchRunner] Could not find randomizer: {typeof(T).Name}. Check Scenario components.");
            return null;
        }
    }
}