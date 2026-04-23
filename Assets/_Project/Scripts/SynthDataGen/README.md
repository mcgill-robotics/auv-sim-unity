# SynthDataGen Module

The `SynthDataGen` module provides automated synthetic data generation for the AUV simulator using the **Unity Perception** package. It's designed to run batches of procedural simulations, seamlessly altering environments (e.g., lighting, water murkiness, camera positions, object placement) to generate diverse ML training datasets.

## How It Works

The system revolves around the **Unity Perception `FixedLengthScenario`**. The module provides several custom **Randomizers** that tap into the simulation iteration loop, but orchestrates them using the `BatchRunner`, which allows changing configuration "profiles" across iterations without having to restart the simulation.

### Core Components

1. **`BatchRunner` & `BatchConfig`**: 
   - Replaces manual randomizer configuration. You set up a list of `BatchConfigs` (e.g., "Clear Water Batch", "Murky Water Batch", "Background Only Batch") in the `BatchRunner` inspector.
   - The `BatchRunner` intercepts the `FixedLengthScenario`, adds up the total iterations from all batches, and automatically drives the state of the randomizers based on the current iteration slice.
   - Includes a *Test Mode* to easily preview what a specific batch looks like without having to wait for the whole run.

2. **Custom Randomizers** (Execute once per iteration):
   - **`PoolFloorPlacementRandomizer`**: Uses Poisson disk sampling to scatter environment props (bins, torpedo boards, gates) across a defined placement area. Automatically pools and cleans up objects per iteration. **Must be placed first** in the scene's randomizer sequence.
   - **`BoundedCameraRandomizer`**: Randomizes the camera's position strictly within the allowed valid "Pool Volume". If `lookAtSpawnedObject` is active, it will actively hunt down randomly spawned props and look at them, introducing yaw-jitter, pitch angles, and roll (AUV wobble) variation.
   - **`UnderwaterEnvironmentRandomizer`**: Procedurally randomizes HDRP water scattering, absorption depth (murkiness), post-processing filters (color grading/film grain), planar reflection intensity, and sunlight angles to simulate drastically different pool visibility constraints.

3. **Helper Scripts**:
   - **`VisualTargetRandomizer`**: Attaches to specific prefabs (e.g. gates with changing poster tasks). Randomizes materials and explicitly syncs the visual material with the active ground-truth Label, preventing labeling errors.
   - **`ConditionalLabeling`**: Attaches to objects with a Unity Perception `Labeling` component. By default, Unity labels things even if they are out of focus. This script disables the label if the camera is too far away or looking at it from an extreme/invalid angle.

---

## Steps to Make It Work

To use this module in a new scene or to generate datasets:

### 1. Configure the Perception Scenario
1. Create a `GameObject` in the scene (e.g., `SimulationScenario`).
2. Attach a **`FixedLengthScenario`** component to it.
3. Add the three custom randomizers to the Scenario Randomizer list:
   - *PoolFloorPlacementRandomizer* (Make sure to populate `Prefab Configs` with ML training props like gates/bins and their respective Y-spawning heights).
   - *BoundedCameraRandomizer* (Set references, make sure this evaluates **after** the Placement Randomizer).
   - *UnderwaterEnvironmentRandomizer* (Assign the scene's `Global Volume`, HDRP `Water Surface`, `SunLight`, and `Planar Reflection Probe`).

### 2. Set Up the Batch Runner
1. Attach the `BatchRunner` script onto the same object as the `FixedLengthScenario`.
2. Ensure you have the required components linked in the Randomizers, as the `BatchRunner` will hunt for them dynamically during `Awake()`.
3. Open the `Batch Configs` list in the `BatchRunner` inspector and add configurations. For each config, set the `Iterations` and tune settings like `Color Filter Ranges` or `Camera Jitter`.

### 3. Generate Data
1. You can enable `Test Mode` in the `BatchRunner` inspector and provide a `Test Batch Index` if you simply want to dial in your parameters visually over a short loop.
2. Ensure your Unity Perception **DatasetCapture** options are correctly routed to your output folder and JSON dump options.
3. Hit **Play** in the Unity Editor. The `BatchRunner` will sum up all iterations, take over the scenario loop, and smoothly apply boundaries and physical property changes as batches switch, outputting your dataset!

---

## How to Modify/Extend This Module

* **Adding New Parameters to Randomizers**: If you create a new variable in `UnderwaterEnvironmentRandomizer` (e.g. `Light Color`), you must create a corresponding `Vector2` range or parameter inside `BatchConfig.cs` and wire the parameter update from the sampler map inside `BatchRunner.UpdateBatchConfig(...)`.
* **Adding New Randomizers**: If you introduce a completely entirely new randomizer (e.g. `FloatingParticlesRandomizer`):
  1. Have your new Randomizer inherit from `Randomizer`.
  2. Map its values inside `BatchRunner`. Cache it in `Awake()` via `GetRandomizer<MyNewRandomizer>()`.
  3. Change its properties dynamically inside `BatchRunner.UpdateBatchConfig()`.
* **Changing Object Placement Constraints**: Open `PoolFloorPlacementRandomizer` and configure `spawnHeight` behavior, or move to a non-planar 3D volumetric spawn approach by migrating off of the basic `PoissonDiskSampling` into 3-axis math points.

## Adding a New Visually Randomized Prop

1. Create a new prefab for the prop (e.g., a new type of gate or bin).
2. Add a `VisualTargetRandomizerTag` component to the root of prefab.
3. Determine which parts of the prehab should have randomized configurations (e.g., a gate's image material, or a bin's body color). 
4. Add each part found above to the `targets` list of the `VisualTargetRandomizerTag`. Ensure the target has the correct world transform (i.e. position/rotation/scale relative to the prop root).
5. Determine all visual variations that can be applied to the targets.
6. Make each visual variation as a prehab in a subfolder of the appropriate prop folder. One easy way this can be done is to create the visual variant as a child of one of your targets, and then drag that child into the Unity Project window to create a prefab out of it.
7. For each variant prefab created, add it to the `configs` list of the `VisualTargetRandomizer Tag` of the prop.
8. Test your new prop by pressing the "Randomize Materials & Labels Now" button on the `VisualTargetRandomizerTag`. You should see each target being assigned a new config.

Further notes to consider:

- To add labels, add a `Labeling` component to the target game object. It must have a `MeshRenderer` and `Material` component so that the Perception Camera can capture the label in the dataset.
- Add a `ConditionalLabeling` component to the same target game object if you want to disable the label under certain conditions (e.g., if the camera is too far away or looking at it from an extreme angle).
- If you want to add a label to a specific portion of the prop without adding a new visible material (i.e. sublabelling an already existing label), add the `Transparent` material to the object along with the `MeshRenderer` and `Labeling` components. This way, the Perception Camera can capture the label without affecting the visual appearance of the prop.

## Reproducibility and Random State

All randomization is driven by a `Unity.Mathematics.Random` state, which is seeded from the `SamplerState` of the `FixedLengthScenario`. This ensures that all randomizers in the scenario are perfectly in sync and reproducible across runs, as long as the same seed and batch configurations are used. Please respect the following patterns to ensure reproducibility:

1. Avoid reordering public Parameter fields in any Randomizer subclass and reordering Randomizers in the `FixedLengthScenario` stack, as this will change the order of random state consumption and break reproducibility. Reordering is acceptable if a entirely new random dataset generation is desired (i.e. a refactor of the dataset). A similar caveat applies to conditionally consuming the random state (e.g. only generating random values if a checkbox is enabled) — this will also change the ordering of random state and break reproducibility when toggling the checkbox on/off between runs.
2. **Never** use `UnityEngine.Random` or `System.Random` — use `SamplerState.NextRandomState()` instead to seed a `Unity.Mathematics.Random` instance, and use that for all random value generation:

```csharp
// ✅ Correct way to generate a random float between 0 and 1, random state should be a class member reinitialized in OnIterationStart()
Unity.Mathematics.Random RandomState = new Unity.Mathematics.Random(SamplerState.NextRandomState());
float randomValue = RandomState.NextFloat(0f, 1f);
// ❌ Incorrect way (breaks reproducibility)
float randomValue = UnityEngine.Random.Range(0f, 1f);
```

Since the image capture time is non deterministic (depends on the Unity rendering loop and the hardware), the images themselves will be ever imperceptibly different across runs even with the same random seed. Thus, to verify if seeding worked, one can generate two datasets with the same seed and batch config, then compare the JSON metadata dumps — they should be identical in terms of randomizer parameter values and captured labels. Go to the dataset directory (typical `~/.config/unity3d/McGill\ Robotics/AUV-SIM-UNITY`) and recursively diff the two datasets:

```bash
diff -r --exclude="*.png" solo solo_1
```

The only difference between the two directories should be simulation start and end times.
