using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Alias to resolve ambiguity with UnityEngine.Rendering.FloatParameter
using PerceptionFloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;
using ColorRgbParameter = UnityEngine.Perception.Randomization.Parameters.ColorRgbParameter;

/// <summary>
/// Randomizes underwater environment properties for synthetic data generation.
/// Controls water visibility, color, and camera sensor/color grading effects.
/// </summary>
[Serializable]
[AddRandomizerMenu("RoboSub/Underwater Environment Randomizer")]
public class UnderwaterEnvironmentRandomizer : Randomizer
{
    #region References
    
    [Header("Scene References")]
    [Tooltip("The Global Volume controlling Post-Processing (Fog, Exposure, Grain)")]
    public Volume globalVolume;
    
    [Tooltip("The HDRP Water Surface component")]
    public WaterSurface waterSurface;
    
    #endregion
    
    #region Water Parameters
    
    [Header("Water Visibility")]
    [Tooltip("Low values (3-8m) = Murky/Muddy. High values (20-50m) = Crystal Clear.")]
    public PerceptionFloatParameter absorptionDistance = new PerceptionFloatParameter { value = new UniformSampler(4f, 20f) };
    
    [Header("Water Color")]
    [Tooltip("Simulates different water chemical balances/algae (Teal vs Green vs Deep Blue)")]
    public ColorRgbParameter waterScatterColor = new ColorRgbParameter
    {
        // Wide range: from murky brown/green to deep blue to teal
        red = new UniformSampler(0.0f, 0.4f),     // Some red for murky/brown tones
        green = new UniformSampler(0.2f, 0.8f),   // Wide green range
        blue = new UniformSampler(0.3f, 1.0f),    // Wide blue range
        alpha = new ConstantSampler(1f)
    };
    
    #endregion
    
    #region Color Grading Parameters
    
    [Header("Color Grading")]
    [Tooltip("Post Exposure: Brightness adjustment (-1 to 1)")]
    public PerceptionFloatParameter postExposure = new PerceptionFloatParameter { value = new UniformSampler(-1f, 1f) };
    
    [Tooltip("Contrast: Image contrast (0 = flat, 100 = high contrast). Default ~12")]
    public PerceptionFloatParameter contrast = new PerceptionFloatParameter { value = new UniformSampler(-30f, 25f) };
    
    [Tooltip("Saturation: Color intensity (-100 = grayscale, 0 = normal). Default ~-30")]
    public PerceptionFloatParameter saturation = new PerceptionFloatParameter { value = new UniformSampler(-80f, 0f) };
    
    [Header("Color Filter")]
    [Tooltip("Underwater tint color. Base: #8FDCEC (teal/cyan)")]
    public ColorRgbParameter colorFilter = new ColorRgbParameter
    {
        // Wide range: from warm teal to cool blue to greenish
        red = new UniformSampler(0.3f, 0.8f),     // Allows warmer and cooler tones
        green = new UniformSampler(0.5f, 1.0f),   // Green-blue spectrum
        blue = new UniformSampler(0.6f, 1.0f),    // Always some blue tint
        alpha = new ConstantSampler(1f)
    };
    
    #endregion
    
    #region Sensor Noise Parameters
    
    [Header("Sensor Noise")]
    [Tooltip("Simulates high ISO noise on the camera sensor (0 = clean, 1 = very noisy)")]
    public PerceptionFloatParameter filmGrainIntensity = new PerceptionFloatParameter { value = new UniformSampler(0.0f, 1.0f) };
    
    #endregion
    
    #region Private Fields
    
    private ColorAdjustments _colorAdjustments;
    private FilmGrain _filmGrain;
    
    #endregion
    
    #region Randomizer Lifecycle
    
    protected override void OnAwake()
    {
        // Cache Volume profile overrides
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _colorAdjustments);
            globalVolume.profile.TryGet(out _filmGrain);
        }
    }

    protected override void OnIterationStart()
    {
        // 1. Randomize Physical Water Appearance
        if (waterSurface != null)
        {
            // Changes transparency/murkiness
            waterSurface.absorptionDistance = absorptionDistance.Sample();
            
            // Sync scattering and refraction color for realistic lighting
            Color sampledColor = waterScatterColor.Sample();
            waterSurface.scatteringColor = sampledColor;
            waterSurface.refractionColor = sampledColor;
        }

        // 2. Randomize Color Grading
        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.value = postExposure.Sample();
            _colorAdjustments.contrast.value = contrast.Sample();
            _colorAdjustments.saturation.value = saturation.Sample();
            _colorAdjustments.colorFilter.value = colorFilter.Sample();
        }
        
        // 3. Randomize Film Grain (sensor noise)
        if (_filmGrain != null)
        {
            _filmGrain.intensity.value = filmGrainIntensity.Sample();
        }
    }
    
    #endregion
}

