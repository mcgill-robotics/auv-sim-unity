using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;

// Alias to match UnderwaterEnvironmentRandomizer
using PerceptionFloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;

/// <summary>
/// Manages automatic execution of multiple batch configurations in sequence.
/// Applies batch settings to randomizers and handles batch transitions.
/// </summary>
public class BatchRunner : MonoBehaviour
{
    #region Settings
    
    [Header("Batch Configurations")]
    [Tooltip("List of batch configurations to run")]
    public BatchConfig[] batchConfigs;
    
    [Header("Run Mode")]
    [Tooltip("Which batch to run in Test mode (0-indexed)")]
    public int testBatchIndex = 0;
    
    [Tooltip("If true, runs only the test batch. If false, runs all batches in sequence.")]
    public bool testModeEnabled = true;
    
    #endregion
    
    #region Runtime State
    
    private int _currentBatchIndex = 0;
    private FixedLengthScenario _scenario;
    private PoolFloorPlacementRandomizer _placementRandomizer;
    private UnderwaterEnvironmentRandomizer _underwaterRandomizer;
    private BoundedCameraRandomizer _cameraRandomizer;
    
    private bool _isRunning = false;
    private float _originalSeparationDistance;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Cache references
        _scenario = FindObjectOfType<FixedLengthScenario>();
        
        if (_scenario == null)
        {
            Debug.LogError("[BatchRunner] No FixedLengthScenario found in scene!");
            enabled = false;
            return;
        }
        
        // Find randomizers
        try
        {
            _placementRandomizer = _scenario.GetRandomizer<PoolFloorPlacementRandomizer>();
            _originalSeparationDistance = _placementRandomizer.separationDistance;
        }
        catch { Debug.LogWarning("[BatchRunner] PoolFloorPlacementRandomizer not found"); }
        
        try
        {
            _underwaterRandomizer = _scenario.GetRandomizer<UnderwaterEnvironmentRandomizer>();
        }
        catch { Debug.LogWarning("[BatchRunner] UnderwaterEnvironmentRandomizer not found"); }
        
        try
        {
            _cameraRandomizer = _scenario.GetRandomizer<BoundedCameraRandomizer>();
        }
        catch { Debug.LogWarning("[BatchRunner] BoundedCameraRandomizer not found"); }
    }
    
    private void Start()
    {
        if (batchConfigs == null || batchConfigs.Length == 0)
        {
            Debug.LogError("[BatchRunner] No batch configurations assigned!");
            return;
        }
        
        // Start with first batch (or test batch)
        _currentBatchIndex = testModeEnabled ? testBatchIndex : 0;
        _currentBatchIndex = Mathf.Clamp(_currentBatchIndex, 0, batchConfigs.Length - 1);
        
        ApplyBatchConfig(batchConfigs[_currentBatchIndex]);
        _isRunning = true;
        
        Debug.Log($"[BatchRunner] Starting batch: {batchConfigs[_currentBatchIndex].batchName}");
    }
    
    private void Update()
    {
        if (!_isRunning || _scenario == null) return;
        
        // Check if current batch completed
        if (_scenario.state == ScenarioBase.State.Idle)
        {
            OnBatchComplete();
        }
    }
    
    #endregion
    
    #region Batch Management
    
    private void OnBatchComplete()
    {
        Debug.Log($"[BatchRunner] Batch '{batchConfigs[_currentBatchIndex].batchName}' completed");
        
        // If test mode or last batch, stop
        if (testModeEnabled || _currentBatchIndex >= batchConfigs.Length - 1)
        {
            Debug.Log("[BatchRunner] All batches complete!");
            _isRunning = false;
            return;
        }
        
        // Advance to next batch
        _currentBatchIndex++;
        ApplyBatchConfig(batchConfigs[_currentBatchIndex]);
        
        Debug.Log($"[BatchRunner] Starting batch: {batchConfigs[_currentBatchIndex].batchName}");
        
        // Restart scenario for next batch
        _scenario.Restart();
    }
    
    private void ApplyBatchConfig(BatchConfig config)
    {
        if (config == null)
        {
            Debug.LogError("[BatchRunner] Null batch config!");
            return;
        }
        
        // Apply iterations
        _scenario.constants.iterationCount = config.iterations;
        
        // Apply Placement settings
        if (_placementRandomizer != null)
        {
            _placementRandomizer.enabled = config.spawnObjects;
            if (config.spawnObjects)
            {
                _placementRandomizer.separationDistance = config.separationDistance;
            }
        }
        
        // Apply Underwater settings
        if (_underwaterRandomizer != null)
        {
            _underwaterRandomizer.absorptionDistance = new PerceptionFloatParameter
            {
                value = new UniformSampler(config.absorptionDistanceMin, config.absorptionDistanceMax)
            };
            
            _underwaterRandomizer.filmGrainIntensity = new PerceptionFloatParameter
            {
                value = new UniformSampler(config.filmGrainMin, config.filmGrainMax)
            };
        }
        
        // Apply Camera settings
        if (_cameraRandomizer != null)
        {
            _cameraRandomizer.lookAtSpawnedObject = config.lookAtSpawnedObject;
            _cameraRandomizer.minDistance = config.cameraMinDistance;
            _cameraRandomizer.maxDistance = config.cameraMaxDistance;
            
            _cameraRandomizer.pitch = new PerceptionFloatParameter
            {
                value = new UniformSampler(config.pitchMin, config.pitchMax)
            };
        }
        
        Debug.Log($"[BatchRunner] Applied config: {config.batchName} ({config.iterations} iterations)");
    }
    
    #endregion
}
