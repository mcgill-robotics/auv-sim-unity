using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Editor window to bake a spherical lookup table of projected areas AND center of pressure offsets.
/// Uses GPU rasterization with an orthographic camera to handle occlusion correctly.
/// 
/// The center of pressure offset allows realistic drag torques when moving at angles.
/// 
/// Access via: Tools → AUV Sim → Bake Drag Lookup Table
/// </summary>
public class DragLookupBakerWindow : EditorWindow
{
    [SerializeField] private GameObject targetRoot;
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private int renderResolution = 256;
    [SerializeField] private int longitudeSamples = 64;
    [SerializeField] private int latitudeSamples = 32;
    [SerializeField] private string outputPath = "_Project/Data/drag_lookup.json";
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private int samplesPerFrame = 5;  // How many samples per editor frame

    private const int TEMP_LAYER = 31;
    private static readonly Vector3 BAKE_ORIGIN = new Vector3(0, 1000f, 0);  // Far from scene geometry
    
    // Preview
    private Texture2D previewTexture;
    private string statusMessage = "";
    private float lastArea = 0f;
    private Vector2 lastCentroid = Vector2.zero;
    private float progress = 0f;
    
    // Baking state (for async processing)
    private bool isBaking = false;
    private BakeState bakeState;

    private class BakeState
    {
        public GameObject bakeRoot;
        public Camera bakeCam;
        public RenderTexture rt;
        public Material whiteMat;
        public Texture2D readbackTex;
        public float[] areaData;
        public float[] offsetXData;
        public float[] offsetYData;
        public int currentSample;
        public int totalSamples;
        public float pixelArea;
        public float pixelWorldSize;
        public float halfRes;
        public float cameraDistance;
        public float orthoSize;
    }

    [MenuItem("Tools/AUV Sim/Bake Drag Lookup Table")]
    public static void ShowWindow()
    {
        var window = GetWindow<DragLookupBakerWindow>("Drag Lookup Baker");
        window.minSize = new Vector2(400, 580);
    }

    private void OnGUI()
    {
        GUILayout.Label("Drag Lookup Table Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Bakes projected areas AND center of pressure offsets for hydrodynamic drag.\n" +
            "The Rigidbody is used to get the center of mass position.",
            MessageType.Info);
        
        EditorGUILayout.Space(10);

        // Target
        GUI.enabled = !isBaking;
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("Root GameObject", targetRoot, typeof(GameObject), true);
        targetRigidbody = (Rigidbody)EditorGUILayout.ObjectField("Rigidbody (for CoM)", targetRigidbody, typeof(Rigidbody), true);
        
        if (targetRoot != null && targetRigidbody == null)
        {
            targetRigidbody = targetRoot.GetComponentInParent<Rigidbody>();
            if (targetRigidbody == null)
                targetRigidbody = targetRoot.GetComponentInChildren<Rigidbody>();
        }

        EditorGUILayout.Space(5);

        // Settings
        EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);
        renderResolution = EditorGUILayout.IntSlider("Render Resolution", renderResolution, 64, 512);
        longitudeSamples = EditorGUILayout.IntSlider("Longitude Samples", longitudeSamples, 8, 128);
        latitudeSamples = EditorGUILayout.IntSlider("Latitude Samples", latitudeSamples, 4, 64);
        samplesPerFrame = EditorGUILayout.IntSlider("Samples Per Frame", samplesPerFrame, 1, 20);

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        verboseLogging = EditorGUILayout.Toggle("Verbose Logging", verboseLogging);
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // Bake/Cancel Button
        bool canBake = targetRoot != null && targetRigidbody != null;
        
        if (isBaking)
        {
            // Show progress bar
            Rect progressRect = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, progress, statusMessage);
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Cancel Baking", GUILayout.Height(30)))
            {
                CancelBaking();
            }
        }
        else
        {
            GUI.enabled = canBake;
            if (GUILayout.Button("Bake Drag Lookup Table", GUILayout.Height(30)))
            {
                StartBaking();
            }
            GUI.enabled = true;
        }

        if (!canBake && !isBaking)
        {
            EditorGUILayout.HelpBox("Assign both Root GameObject and Rigidbody.", MessageType.Warning);
        }

        // Preview with centroid marker
        if (previewTexture != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Area: {lastArea:F4} m² | Offset: ({lastCentroid.x:F3}, {lastCentroid.y:F3})m", EditorStyles.boldLabel);
            
            float previewSize = Mathf.Min(position.width - 20, 200);
            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize);
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
            
            // Draw crosshair for center of mass
            float cx = previewRect.x + previewRect.width / 2;
            float cy = previewRect.y + previewRect.height / 2;
            Handles.color = Color.green;
            Handles.DrawLine(new Vector3(cx - 10, cy, 0), new Vector3(cx + 10, cy, 0));
            Handles.DrawLine(new Vector3(cx, cy - 10, 0), new Vector3(cx, cy + 10, 0));
        }
    }

    private void StartBaking()
    {
        if (targetRoot == null || targetRigidbody == null) return;

        // Find all MeshFilters
        MeshFilter[] meshFilters = targetRoot.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("[DragLookupBaker] No MeshFilters found!");
            return;
        }

        if (verboseLogging)
            Debug.Log($"[DragLookupBaker] Found {meshFilters.Length} meshes");

        Vector3 worldCoM = targetRigidbody.worldCenterOfMass;
        Bounds combinedBounds = CalculateCombinedBounds(meshFilters);
        float maxExtent = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y, combinedBounds.extents.z);

        // Initialize bake state
        bakeState = new BakeState();
        bakeState.cameraDistance = maxExtent * 3f;
        bakeState.orthoSize = maxExtent * 1.5f;
        bakeState.totalSamples = latitudeSamples * longitudeSamples;
        bakeState.currentSample = 0;

        // Create bake environment
        bakeState.bakeRoot = new GameObject("_BakeRoot");
        bakeState.bakeRoot.hideFlags = HideFlags.HideAndDontSave;

        GameObject camGO = new GameObject("_TempBakeCamera");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        camGO.transform.SetParent(bakeState.bakeRoot.transform);
        
        bakeState.bakeCam = camGO.AddComponent<Camera>();
        bakeState.bakeCam.orthographic = true;
        bakeState.bakeCam.orthographicSize = bakeState.orthoSize;
        bakeState.bakeCam.nearClipPlane = 0.01f;
        bakeState.bakeCam.farClipPlane = bakeState.cameraDistance * 2f;
        bakeState.bakeCam.clearFlags = CameraClearFlags.SolidColor;
        bakeState.bakeCam.backgroundColor = Color.black;
        bakeState.bakeCam.cullingMask = 1 << TEMP_LAYER;
        bakeState.bakeCam.enabled = false;
        bakeState.bakeCam.allowHDR = false;
        bakeState.bakeCam.allowMSAA = false;

        bakeState.rt = new RenderTexture(renderResolution, renderResolution, 24, RenderTextureFormat.ARGB32);
        bakeState.rt.antiAliasing = 1;
        bakeState.rt.Create();
        bakeState.bakeCam.targetTexture = bakeState.rt;

        Shader unlitShader = Shader.Find("Unlit/Color");
        bakeState.whiteMat = new Material(unlitShader);
        bakeState.whiteMat.color = Color.white;
        bakeState.whiteMat.hideFlags = HideFlags.HideAndDontSave;

        // Create mesh copies centered on CoM
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            GameObject copy = new GameObject($"_Copy_{mf.name}");
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.transform.SetParent(bakeState.bakeRoot.transform);
            copy.layer = TEMP_LAYER;

            MeshFilter copyMF = copy.AddComponent<MeshFilter>();
            MeshRenderer copyMR = copy.AddComponent<MeshRenderer>();
            
            copyMF.sharedMesh = mf.sharedMesh;
            copyMR.sharedMaterial = bakeState.whiteMat;
            copyMR.shadowCastingMode = ShadowCastingMode.Off;
            copyMR.receiveShadows = false;

            // Position relative to CENTER OF MASS, but offset to BAKE_ORIGIN to avoid scene interference
            copy.transform.position = BAKE_ORIGIN + (mf.transform.position - worldCoM);
            copy.transform.rotation = mf.transform.rotation;
            copy.transform.localScale = mf.transform.lossyScale;
        }

        // Prepare data arrays
        int numSamples = bakeState.totalSamples;
        bakeState.areaData = new float[numSamples];
        bakeState.offsetXData = new float[numSamples];
        bakeState.offsetYData = new float[numSamples];

        bakeState.readbackTex = new Texture2D(renderResolution, renderResolution, TextureFormat.ARGB32, false);
        
        if (previewTexture != null) DestroyImmediate(previewTexture);
        previewTexture = new Texture2D(renderResolution, renderResolution, TextureFormat.ARGB32, false);  // Must match readbackTex format

        float frustumWidth = bakeState.orthoSize * 2f;
        bakeState.pixelWorldSize = frustumWidth / renderResolution;
        bakeState.pixelArea = bakeState.pixelWorldSize * bakeState.pixelWorldSize;
        bakeState.halfRes = renderResolution / 2f;

        // Warm-up render to clear any stale data in the RenderTexture
        bakeState.bakeCam.transform.position = BAKE_ORIGIN + Vector3.forward * bakeState.cameraDistance;
        bakeState.bakeCam.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        bakeState.bakeCam.Render();  // First render clears the RT

        // Start async baking
        isBaking = true;
        EditorApplication.update += BakeUpdate;
        
        statusMessage = "Starting bake...";
    }

    private void BakeUpdate()
    {
        if (!isBaking || bakeState == null) return;

        // Process multiple samples per frame
        for (int s = 0; s < samplesPerFrame && bakeState.currentSample < bakeState.totalSamples; s++)
        {
            int latIdx = bakeState.currentSample / longitudeSamples;
            int lonIdx = bakeState.currentSample % longitudeSamples;

            // Latitude: +π/2 (top) at latIdx=0, -π/2 (bottom) at latIdx=max
            float lat = Mathf.PI * (0.5f - (float)latIdx / (latitudeSamples - 1));
            float lon = 2f * Mathf.PI * ((float)lonIdx / longitudeSamples);

            Vector3 viewDir = new Vector3(
                Mathf.Cos(lat) * Mathf.Sin(lon),
                Mathf.Sin(lat),
                Mathf.Cos(lat) * Mathf.Cos(lon)
            );

            // Camera orbits around BAKE_ORIGIN (where mesh copies are centered)
            bakeState.bakeCam.transform.position = BAKE_ORIGIN + viewDir * bakeState.cameraDistance;
            
            // Explicitly match Drag.cs basis construction to ensure consistency
            // Camera looks AT origin, so forward is -viewDir
            // Up vector depends on view angle (pole handling)
            Vector3 camFwd = -viewDir;
            Vector3 upHint = Mathf.Abs(viewDir.y) > 0.99f ? Vector3.forward : Vector3.up;
            bakeState.bakeCam.transform.rotation = Quaternion.LookRotation(camFwd, upHint);
            
            bakeState.bakeCam.Render();

            RenderTexture.active = bakeState.rt;
            bakeState.readbackTex.ReadPixels(new Rect(0, 0, renderResolution, renderResolution), 0, 0);
            bakeState.readbackTex.Apply();
            RenderTexture.active = null;

            Color32[] pixels = bakeState.readbackTex.GetPixels32();
            int whiteCount = 0;
            float sumX = 0f, sumY = 0f;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r > 50 || pixels[i].g > 50 || pixels[i].b > 50)
                {
                    whiteCount++;
                    sumX += i % renderResolution;
                    sumY += i / renderResolution;
                }
            }

            float projectedArea = whiteCount * bakeState.pixelArea;
            
            float offsetX = 0f, offsetY = 0f;
            if (whiteCount > 0)
            {
                offsetX = (sumX / whiteCount - bakeState.halfRes) * bakeState.pixelWorldSize;
                offsetY = (sumY / whiteCount - bakeState.halfRes) * bakeState.pixelWorldSize;
            }

            bakeState.areaData[bakeState.currentSample] = projectedArea;
            bakeState.offsetXData[bakeState.currentSample] = offsetX;
            bakeState.offsetYData[bakeState.currentSample] = offsetY;

            lastArea = projectedArea;
            lastCentroid = new Vector2(offsetX, offsetY);
            
            bakeState.currentSample++;
        }

        // Update preview texture
        Graphics.CopyTexture(bakeState.readbackTex, previewTexture);
        
        // Calculate current latitude for display (same inverted formula as sample loop)
        int displayLatIdx = Mathf.Max(0, bakeState.currentSample - 1) / longitudeSamples;
        float displayLat = Mathf.PI * (0.5f - (float)displayLatIdx / (latitudeSamples - 1));
        float latDegrees = displayLat * Mathf.Rad2Deg;
        
        progress = (float)bakeState.currentSample / bakeState.totalSamples;
        statusMessage = $"{bakeState.currentSample}/{bakeState.totalSamples} | Lat: {latDegrees:F0}° | Area: {lastArea:F4}m²";
        
        Repaint();

        // Check if done
        if (bakeState.currentSample >= bakeState.totalSamples)
        {
            FinishBaking();
        }
    }

    private void FinishBaking()
    {
        EditorApplication.update -= BakeUpdate;

        // Save
        SaveLookupTable(bakeState.areaData, bakeState.offsetXData, bakeState.offsetYData);

        // Stats
        float maxArea = 0f, minArea = float.MaxValue, maxOffset = 0f;
        for (int i = 0; i < bakeState.totalSamples; i++)
        {
            if (bakeState.areaData[i] > maxArea) maxArea = bakeState.areaData[i];
            if (bakeState.areaData[i] < minArea && bakeState.areaData[i] > 0) minArea = bakeState.areaData[i];
            float offsetMag = Mathf.Sqrt(bakeState.offsetXData[i] * bakeState.offsetXData[i] + bakeState.offsetYData[i] * bakeState.offsetYData[i]);
            if (offsetMag > maxOffset) maxOffset = offsetMag;
        }

        statusMessage = $"Done! Area: {minArea:F4}-{maxArea:F4}m², Max offset: {maxOffset:F3}m";
        Debug.Log($"[DragLookupBaker] Complete! Area: {minArea:F4} to {maxArea:F4} m², Max offset: {maxOffset:F3} m");

        // Cleanup
        CleanupBakeState();
        isBaking = false;
    }

    private void CancelBaking()
    {
        EditorApplication.update -= BakeUpdate;
        CleanupBakeState();
        isBaking = false;
        statusMessage = "Cancelled";
    }

    private void CleanupBakeState()
    {
        if (bakeState == null) return;

        if (bakeState.bakeRoot != null) DestroyImmediate(bakeState.bakeRoot);
        if (bakeState.whiteMat != null) DestroyImmediate(bakeState.whiteMat);
        if (bakeState.readbackTex != null) DestroyImmediate(bakeState.readbackTex);
        if (bakeState.rt != null)
        {
            bakeState.rt.Release();
            DestroyImmediate(bakeState.rt);
        }
        bakeState = null;
    }

    private Bounds CalculateCombinedBounds(MeshFilter[] meshFilters)
    {
        bool hasBounds = false;
        Bounds combined = new Bounds();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            if (!hasBounds)
            {
                combined = mr.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(mr.bounds);
            }
        }
        return combined;
    }

    private void SaveLookupTable(float[] areaData, float[] offsetXData, float[] offsetYData)
    {
        DragLookupData data = new DragLookupData
        {
            longitudeSamples = longitudeSamples,
            latitudeSamples = latitudeSamples,
            flatData = areaData,
            offsetXData = offsetXData,
            offsetYData = offsetYData
        };

        string json = JsonUtility.ToJson(data, true);
        string fullPath = Path.Combine(Application.dataPath, outputPath);
        
        string directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();
        
        Debug.Log($"[DragLookupBaker] Saved to: {fullPath}");
        
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/" + outputPath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
    }

    private void OnDestroy()
    {
        if (isBaking) CancelBaking();
        if (previewTexture != null) DestroyImmediate(previewTexture);
    }
}
