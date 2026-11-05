# Synthetic Data Generation Setup Guide

This guide covers the physical setup and workflow for generating synthetic datasets using the Unity Perception package in this simulator. The setup is designed to be highly dynamic, making it easy to swap out props and adapt to different RoboSub competition seasons.

## 1. Preparing Props and Assets

Before generating data, you need to ensure the 3D models (props/gates/bins) are registered with the Unity Perception system so they can be identified in the generated images.

### Adding Labels to a Prop
1. Open or select the prefab for your prop (e.g., a competition gate or buoy).
2. Add a **`Labeling`** component to the root of the prefab.
3. In the `Labeling` component, add a new string to the `Labels` list that exactly matches what you want the class name to be (e.g., `gate_red`, `bin_cover`).
4. Apply the prefab overrides if you modified the object in the scene.

### Optional: Conditional Labeling & Visual Randomization
If your prop has distinct faces (like a task board or gate poster) that change, or should only be detected from the front:
- Attach the **`VisualTargetRandomizer`** script to randomly swap the displayed material (and its corresponding label) across iterations.
- Attach the **`ConditionalLabeling`** script to ensure the object is only labeled when the camera is viewing it from a valid angle and within a maximum distance. This prevents generating "garbage" labels for objects the model can barely see.

## 2. Configuring the Scenario

Once your props are labeled, open the appropriate data generation scene:
- **Forward Camera:** `Assets/_Project/Scenes/Synthetic_Data_Generation_Scenes/SynthDataGen_RoboSub2026.unity`
- **Downward Camera:** `Assets/_Project/Scenes/Synthetic_Data_Generation_Scenes/SynthDataGen_RoboSub2026_DownCam.unity`

You must then configure the environment and camera.

1. **Resolution Configuration:**
   It is crucial that the synthetic images match the dimensions of the real-world cameras used on the AUV, because Unity Perception captures images directly at the **Game View** tab resolution:
   - Go to the **Game** tab in the Unity Editor.
   - Click the Aspect Ratio dropdown (usually says "Free Aspect" or "16:9").
   - Click the **+** button at the bottom to add a fixed resolution preset if not already present:
     - **Front Camera Scene (`SynthDataGen_RoboSub2026.unity`):** **`672 × 376`** (16:9, matching the physical ZED 2i).
     - **Downward Camera Scene (`SynthDataGen_RoboSub2026_DownCam.unity`):** **`640 × 480`** (4:3, matching the downward camera).
   - Ensure the matching resolution is selected in the Game View dropdown before pressing Play.

2. **Prop Placement Configuration:**
   - Locate the `SimulationScenario` object in your scene.
   - Find the `PoolFloorPlacementRandomizer` component in the Inspector.
   - Expand the `Prefab Configs` list.
   - Assign your labeled prefabs to the list and set their appropriate `Spawn Height` (e.g., `0` for floating objects, `-2.1` for floor objects).

## 3. Running the Generator

The generation process is handled entirely within the Unity Editor.

1. Select your `BatchRunner` component on the `SimulationScenario` object.
2. Configure your desired batches in the `Batch Configs` list (setting varying degrees of water visibility, camera jitter, and iteration counts).
3. **Optional (Quick Preview / Test Mode):** To visually preview camera jitter, murky lighting, and prop placement without running a full multi-thousand frame capture, check **Test Mode** on `BatchRunner` and set **Test Batch Index** to the batch profile you want to test.
4. Verify that the Unity Perception `Perception Camera` component settings (attached to the Main Camera) are pointing to your desired output directory.
5. Press **Play** in the Unity Editor. The `BatchRunner` will run through all configurations and automatically stop when finished.

## 4. Converting to YOLO Format

Unity Perception generates datasets in a specific JSON format (SOLO). To train standard object detection models, you convert this to YOLO format using the built-in converter window:

1. Once data generation completes, stay in the Unity Editor.
2. Open the converter via top menu: **`Tools > RoboSub > YOLO Converter`**.
3. In the window, verify:
   - **Export Format:** Bounding Box (or Segmentation if polygon masks are required).
   - **Image Resolution:** Must match the Game View capture resolution (`672, 376` for front camera, or `640, 480` for downward camera) so normalized coordinates align correctly.
4. Click **Convert Latest Dataset** (or browse to a specific custom dataset folder).
5. The tool outputs a standardized YOLO dataset with images and `.txt` labels ready for model training.
