# Patches relative to upstream

Upstream is `com.unity.perception` 1.0.0-preview.1, git tag `1.0.0-preview.1` (commit `f45895f7`) of
https://github.com/Unity-Technologies/com.unity.perception. The first fork commit copies the published registry
tarball over the tag so the baseline is exactly what Package Manager downloads. The tag and the tarball have the
same code; they only differ in registry metadata, line endings and a few doc pages.

No `.meta` file was added, removed or changed except `PATCHES.md.meta` for this file. Every asset GUID is the same
as upstream, so scenes and prefabs that reference Perception assets keep working.

## 1. `GraphicsSettings.renderPipelineAsset` -> `defaultRenderPipeline`

File: `Editor/GroundTruth/PerceptionCameraEditor.cs`

`renderPipelineAsset` is obsolete in Unity 6 (CS0618, tagged UnityUpgradable). `defaultRenderPipeline` exists
since 2021.2, so no `#if` is needed. Unity's API Updater made this exact change by itself in the AUV sim's
embedded copy.

## 2. Screen blit in `PerceptionUpdater`

File: `Runtime/GroundTruth/PerceptionUpdater.cs`

Upstream, `OnEndContextRendering` always called `BlitVisualizedPerceptionCameraToScreen`, which copies the RGB
channel of the visualized PerceptionCamera (or the one with the highest camera depth) to the back buffer. The AUV
sim had this call commented out with no note.

Why it broke on Unity 6 / HDRP 17:

- The RGB channel is captured by `CameraSensorHdrpPass`, a custom pass at `AfterPostProcess`.
- HDRP 17 renders Screen Space Overlay canvases itself (`SupportedRenderingFeatures.rendersUIOverlay = true`,
  `RenderScreenSpaceOverlayUI` at the end of the camera's render graph). That happens after the custom pass and
  before `endContextRendering`.
- So the blit copied a pre-UI image over the finished frame and erased every overlay canvas, including
  Perception's own segmentation overlay (`OverlayPanel` uses a Screen Space Overlay canvas). Upstream shipped
  that overlay for 2021.3 / HDRP 12, so on that version the UI must have been drawn after `endContextRendering`.
  We did not check HDRP 12 directly.
- If the PerceptionCamera renders into a `targetTexture`, the RGB channel is that texture, and the blit replaced
  the whole game view with the sensor image, hiding whatever the real main camera drew.

The blit is only needed when the PerceptionCamera does not draw to the screen on its own. That happens when a
sensor turns the camera into an HDRP fullscreen passthrough camera: `UnityCameraSensor` with super sampling, and
`CubemapCameraSensor`. In every other case HDRP already drew the same image.

Fix: the blit now runs only if the chosen camera has no `targetTexture` and its `HDAdditionalCameraData` has
`fullscreenPassthrough` set. It also uses `TryGetChannel` instead of `GetChannel`, so it does not throw if a frame
renders before `PerceptionCamera.Start` has enabled the RGB channel.

Limitation: in the passthrough modes, overlay UI is still lost on that camera's screen output, same as before.

## 3. Obsolete API replacements

These removed the CS0618 warnings seen in the AUV sim's Editor.log. Behaviour is unchanged.

| File | Old | New |
| --- | --- | --- |
| `Runtime/GroundTruth/Labelers/JointLabel.cs` | `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` |
| `Editor/GroundTruth/PerceptionCameraEditor.cs` | `FindObjectOfType`, `FindObjectsOfType` | `FindFirstObjectByType`, `FindObjectsByType(FindObjectsSortMode.InstanceID)` |
| `Runtime/PerceptionAnalytics.cs` | `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.InstanceID)` |
| `Runtime/GroundTruth/LabelManagement/LabelManager.cs` | `RenderPipelineManager.beginFrameRendering` | `beginContextRendering` (handler takes `List<Camera>`) |
| `Runtime/GroundTruth/SimulationState.cs` | `RenderPipelineManager.beginFrameRendering` | `beginContextRendering` (handler takes `List<Camera>`) |
| `Runtime/GroundTruth/ImageEncoding/ImageEncoder.cs` | implicit `NativeList` -> `NativeArray` | `.AsArray()` |
| `Runtime/GroundTruth/Labelers/Keypoints/KeypointLabeler.cs` | implicit `NativeList` -> `NativeArray` | `.AsArray()` |
| `Runtime/GroundTruth/Labelers/Occlusion/OcclusionLabeler.cs` | implicit `NativeList` -> `NativeArray` | `.AsArray()` |
| `Runtime/GroundTruth/Utilities/RenderedObjectInfoComputer.cs` | implicit `NativeList` -> `NativeArray` | `.AsArray()` |
| `Runtime/Randomization/Randomizers/RandomizerExamples/Utilities/PoissonDiskSampling.cs` | implicit `NativeList` -> `NativeArray` | `.AsArray()` |
| `Runtime/RandomizerLibrary/Light/LightRandomizerTag.cs` | `HDAdditionalLightData.SetIntensity(v, lightUnit)` | same `LightUnitUtils.ConvertIntensity` call HDRP 17 makes internally, applied to `Light.intensity` |
| `Editor/GroundTruth/LabelConfigEditor.cs` | `ListView.onSelectionChange` | `selectionChanged` |
| `Tests/Runtime/...` (3 files) | `FindObjectOfType`, `FindObjectsOfType` | `FindFirstObjectByType`, `FindObjectsByType(InstanceID)` |
| `Tests/Runtime/RandomizerLibrary/LightRandomizerTests.cs` | `HDAdditionalLightData.intensity` | `Light.intensity` (what the obsolete property forwarded to) |

`FindObjectsSortMode.InstanceID` keeps the old sort order. Neither `beginFrameRendering` handler reads the camera
list. In `LightRandomizerTag`, `LightUnitUtils` is written fully qualified because a `using UnityEngine.Rendering`
makes `FloatParameter` ambiguous with Perception's own type.

Not changed, still warnings:

- `EditorAnalytics.RegisterEventWithLimit` / `SendEventWithLimit` in `Runtime/PerceptionAnalytics.cs`. The
  replacement is the `IAnalytic` API, which is a rewrite of that file.
- `UxmlFactory` / `UxmlTraits` in `Editor/Randomization/VisualElements/Basic/UIntField.cs`. The replacement is
  `[UxmlElement]` on a partial class.

## 4. `package.json`

- `version`: `1.0.0-preview.1-mcgill.1`. `name` stays `com.unity.perception` because the code loads UXML, USS,
  icons and test scenes from `Packages/com.unity.perception/...` (34 places in 10 files).
- `unity`: `6000.0`, `unityRelease` removed. The code now uses APIs that only exist in Unity 2023.1+.
- `dependencies`: burst 1.8.25, collections 2.6.2, nuget.newtonsoft-json 3.2.1, render-pipelines.core 17.0.4.
  These are what the AUV sim resolves on 6000.0.62f1. HDRP is still not a hard dependency, same as upstream;
  without it the `HDRP_PRESENT` code is compiled out and `PerceptionCamera` logs an error.
- Removed the registry-only `upmCi` and `repository` fields, and the `.signature` file, which signs the
  registry tarball and no longer matches.

## 5. Removed `Documentation~`

107 MB of markdown and images. Unity never imports `~` folders. The docs are online at
https://docs.unity3d.com/Packages/com.unity.perception@1.0/manual/index.html and in the upstream history.
`Samples~` (83 MB) and `Tests` (1.8 MB) are kept.

## Not carried over from the AUV sim's embedded copy

The sim's embedded copy also had about 40 `.mat`, `.asset` and `.unity` files under `Runtime/`, `Samples~/` and
`Tests/` that differ from upstream. These are Unity 6 re-serializing YAML on import (new material `version`,
extra default properties, LF line endings). Unity does this to any embedded or local package and will redo it on
its own, so the fork keeps the upstream files.
