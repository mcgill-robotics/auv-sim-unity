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

Once your props are labeled, you must configure the environment and camera.

1. **Resolution Configuration:**
   It is crucial that the synthetic images match the dimensions of the real-world cameras used on the AUV.
   - Go to the **Game** tab in the Unity Editor.
   - Click the Aspect Ratio dropdown (usually says "Free Aspect" or "16:9").
   - Click the **+** button at the bottom to add a new fixed resolution.
   - Set the resolution to **ZED 2i VGA: 672 x 376**.
   - Ensure this resolution is selected before pressing Play.

2. **Prop Placement Configuration:**
   - Locate the `SimulationScenario` object in your scene.
   - Find the `PoolFloorPlacementRandomizer` component in the Inspector.
   - Expand the `Prefab Configs` list.
   - Assign your labeled prefabs to the list and set their appropriate `Spawn Height` (e.g., `0` for floating objects, `-2.1` for floor objects).

## 3. Running the Generator

The generation process is handled entirely within the Unity Editor.

1. Select your `BatchRunner` component on the `SimulationScenario` object.
2. Configure your desired batches in the `Batch Configs` list (setting varying degrees of water visibility, camera jitter, and iteration counts).
3. Verify that the Unity Perception `Perception Camera` Script settings (usually attached to the Main Camera or a global settings object) are pointing to your desired output folder.
4. Press **Play** in the Unity Editor. The `BatchRunner` will run through all configurations and automatically stop when finished.

## 4. Converting to YOLO Format

Unity Perception generates datasets in a specific JSON format (SOLO). To train standard object detection models, you need to convert this to the YOLO structure.

1. Once the simulation stops and your dataset is created, stay in the Unity Editor.
2. Run the **YOLOConverter tool** via the custom Unity Editor menu you have installed (e.g., usually found at the top menu under `Tools -> YOLO Converter` or similar).
3. Point the tool at your newly generated Perception dataset directory.
4. The tool will parse the bounding boxes and output a standard YOLO-compatible dataset (images and `.txt` label files) ready for training.
