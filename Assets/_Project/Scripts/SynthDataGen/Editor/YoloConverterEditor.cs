using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window for converting Unity Perception SOLO datasets to YOLO format.
/// Access via: Tools > RoboSub > YOLO Converter
/// </summary>
public class YoloConverterWindow : EditorWindow
{
    // Settings
    private float _minVisibility = 0.6f;
    private Vector2 _resolution = new Vector2(960, 600);
    private string _customDatasetPath = "";
    
    // UI State
    private Vector2 _scrollPos;
    
    [MenuItem("Tools/RoboSub/YOLO Converter")]
    public static void ShowWindow()
    {
        var window = GetWindow<YoloConverterWindow>("YOLO Converter");
        window.minSize = new Vector2(350, 400);
    }
    
    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        // Header
        EditorGUILayout.LabelField("SOLO → YOLO Converter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Converts Unity Perception SOLO datasets to YOLO format for training.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Settings Section
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        
        _minVisibility = EditorGUILayout.Slider("Min Visibility", _minVisibility, 0f, 1f);
        EditorGUILayout.HelpBox("Objects with visibility below this threshold are excluded.", MessageType.None);
        
        _resolution = EditorGUILayout.Vector2Field("Image Resolution", _resolution);
        EditorGUILayout.HelpBox("Must match your Game View resolution.", MessageType.None);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Convert Latest Section
        EditorGUILayout.LabelField("Quick Convert", EditorStyles.boldLabel);
        
        string basePath = UnityEngine.Perception.Settings.PerceptionSettings.GetOutputBasePath();
        EditorGUILayout.LabelField("Output Path:", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(basePath, EditorStyles.textField, GUILayout.Height(18));
        
        EditorGUILayout.Space(5);
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Convert Latest Dataset", GUILayout.Height(35)))
        {
            ConvertLatestDataset(basePath);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Open Output Folder"))
        {
            if (Directory.Exists(basePath))
                EditorUtility.RevealInFinder(basePath);
            else
                Debug.LogWarning($"Output folder does not exist: {basePath}");
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Custom Path Section
        EditorGUILayout.LabelField("Convert Specific Dataset", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        _customDatasetPath = EditorGUILayout.TextField(_customDatasetPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select SOLO Dataset", basePath, "");
            if (!string.IsNullOrEmpty(selected))
                _customDatasetPath = selected;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_customDatasetPath));
        if (GUILayout.Button("Convert Selected Dataset", GUILayout.Height(30)))
        {
            ProcessDataset(_customDatasetPath);
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void ConvertLatestDataset(string basePath)
    {
        var dirInfo = new DirectoryInfo(basePath);
        var latestDir = dirInfo.GetDirectories("solo*")
            .OrderByDescending(d => d.CreationTime)
            .FirstOrDefault();

        if (latestDir == null)
        {
            EditorUtility.DisplayDialog("Error", $"No SOLO datasets found in:\n{basePath}", "OK");
            return;
        }

        Debug.Log($"Converting Dataset: {latestDir.FullName}");
        ProcessDataset(latestDir.FullName);
    }

    private void ProcessDataset(string datasetPath)
    {
        if (!Directory.Exists(datasetPath))
        {
            EditorUtility.DisplayDialog("Error", $"Dataset path does not exist:\n{datasetPath}", "OK");
            return;
        }
        
        // Create Output Folders
        string yoloImgPath = Path.Combine(datasetPath, "yolo_images");
        string yoloLblPath = Path.Combine(datasetPath, "yolo_labels");
        Directory.CreateDirectory(yoloImgPath);
        Directory.CreateDirectory(yoloLblPath);

        // Build ID Map
        Dictionary<int, int> idMap = new Dictionary<int, int>();
        string defPath = Path.Combine(datasetPath, "annotation_definitions.json");
        
        if (File.Exists(defPath))
        {
            JObject root = JObject.Parse(File.ReadAllText(defPath));
            var definitions = root["annotationDefinitions"] ?? root["annotation_definitions"];
            
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    if (def["id"]?.ToString() == "bounding box")
                    {
                        var spec = def["spec"];
                        int yoloIndex = 0;
                        foreach (var item in spec)
                        {
                            int soloId = item["label_id"].Value<int>();
                            idMap[soloId] = yoloIndex;
                            Debug.Log($"Mapping: {item["label_name"]} (SOLO ID {soloId}) → YOLO {yoloIndex}");
                            yoloIndex++;
                        }
                    }
                }
            }
        }

        // Process Sequences
        int processedCount = 0;
        foreach (var seqDir in Directory.GetDirectories(datasetPath, "sequence.*"))
        {
            foreach (var jsonFile in Directory.GetFiles(seqDir, "step*.frame_data.json"))
            {
                ConvertFrame(jsonFile, yoloImgPath, yoloLblPath, idMap);
                processedCount++;
            }
        }
        
        Debug.Log($"<color=green>Success!</color> Converted {processedCount} frames to YOLO format.");
        Debug.Log($"Images: {yoloImgPath}");
        Debug.Log($"Labels: {yoloLblPath}");
        
        EditorUtility.DisplayDialog("Conversion Complete", 
            $"Converted {processedCount} frames.\n\nOutput:\n{yoloImgPath}", "OK");
    }

    private void ConvertFrame(string jsonPath, string imgOutDir, string lblOutDir, Dictionary<int, int> idMap)
    {
        JObject root = JObject.Parse(File.ReadAllText(jsonPath));

        // Visibility Map
        Dictionary<int, float> visibility = new Dictionary<int, float>();
        var metrics = root["metrics"];
        if (metrics != null)
        {
            foreach (var m in metrics)
            {
                if (m["@type"]?.ToString().Contains("OcclusionMetric") == true)
                {
                    foreach (var val in m["values"])
                    {
                        visibility[val["instanceId"].Value<int>()] = val["percentVisible"].Value<float>();
                    }
                }
            }
        }

        // Bounding Boxes
        var captures = root["captures"];
        if (captures == null) return;

        foreach (var cap in captures)
        {
            if (cap["@type"]?.ToString().Contains("RGBCamera") == false) continue;

            string filename = cap["filename"].ToString();
            string fullImgSource = Path.Combine(Path.GetDirectoryName(jsonPath), filename);
            
            string flatFileName = Path.GetFileName(filename); 
            string uniqueName = new DirectoryInfo(Path.GetDirectoryName(jsonPath)).Name + "_" + flatFileName;
            string destImgPath = Path.Combine(imgOutDir, uniqueName);
            
            if (File.Exists(fullImgSource))
                File.Copy(fullImgSource, destImgPath, true);

            // Generate Label File
            string txtContent = "";
            var annotations = cap["annotations"];
            if (annotations != null)
            {
                foreach (var ann in annotations)
                {
                    if (ann["@type"]?.ToString().Contains("BoundingBox2DAnnotation") == true)
                    {
                        foreach (var val in ann["values"])
                        {
                            int instId = val["instanceId"].Value<int>();
                            
                            if (visibility.TryGetValue(instId, out float vis) && vis < _minVisibility)
                                continue; 

                            int labelId = val["labelId"].Value<int>();
                            
                            float x = val["origin"][0].Value<float>();
                            float y = val["origin"][1].Value<float>();
                            float w = val["dimension"][0].Value<float>();
                            float h = val["dimension"][1].Value<float>();

                            float centerX = (x + (w / 2.0f)) / _resolution.x;
                            float centerY = (y + (h / 2.0f)) / _resolution.y;
                            float normW = w / _resolution.x;
                            float normH = h / _resolution.y;

                            if (idMap.TryGetValue(labelId, out int yoloId))
                            {
                                txtContent += $"{yoloId} {centerX:F6} {centerY:F6} {normW:F6} {normH:F6}\n";
                            }
                        }
                    }
                }
            }

            string txtName = Path.GetFileNameWithoutExtension(uniqueName) + ".txt";
            File.WriteAllText(Path.Combine(lblOutDir, txtName), txtContent);
        }
    }
}


