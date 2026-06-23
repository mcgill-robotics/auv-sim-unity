using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for converting Unity Perception SOLO datasets to YOLO format.
/// Access via: Tools > RoboSub > YOLO Converter
/// </summary>
public class YoloConverterWindow : EditorWindow
{
    private enum ExportFormat { BoundingBox, Segmentation }

    // Settings
    private ExportFormat _exportFormat = ExportFormat.BoundingBox;
    private float _minVisibility = 0.35f;
    private Vector2 _resolution = new Vector2(672, 376);
    private float _polygonSimplificationTolerance = 2.0f;
    private float _minPolygonArea = 50.0f;
    private bool _fillHoles = true;
    private string _customDatasetPath = "";

    // UI State
    private Vector2 _scrollPos;
    private string _lastProcessedDatasetPath = "";
    private int _lastProcessedFrameCount;
    private int _lastFilteredLabelCount;
    private Dictionary<int, string> _lastYoloClassNames = new Dictionary<int, string>();
    private Dictionary<int, int> _lastClassCounts = new Dictionary<int, int>();

    [MenuItem("Tools/RoboSub/YOLO Converter")]
    public static void ShowWindow()
    {
        var window = GetWindow<YoloConverterWindow>("YOLO Converter");
        window.minSize = new Vector2(350, 450);
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

        _exportFormat = (ExportFormat)EditorGUILayout.EnumPopup("Export Format", _exportFormat);
        EditorGUILayout.HelpBox(
            _exportFormat == ExportFormat.BoundingBox ?
            "Exports bounding boxes. Requires BoundingBox2DLabeler in Perception Camera." :
            "Exports polygons for segmentation. Requires InstanceSegmentationLabeler in Perception Camera.",
            MessageType.None);

        _minVisibility = EditorGUILayout.Slider("Min Visibility", _minVisibility, 0f, 1f);
        EditorGUILayout.HelpBox("Objects with visibility below this threshold are excluded.", MessageType.None);

        _resolution = EditorGUILayout.Vector2Field("Image Resolution", _resolution);
        EditorGUILayout.HelpBox("Must match your Game View resolution.", MessageType.None);

        if (_exportFormat == ExportFormat.Segmentation)
        {
            _polygonSimplificationTolerance = EditorGUILayout.Slider("Polygon Simplification", _polygonSimplificationTolerance, 0f, 10f);
            EditorGUILayout.HelpBox("Higher values reduce the number of polygon points (smaller file size, less accuracy).", MessageType.None);

            _minPolygonArea = EditorGUILayout.Slider("Min Polygon Area", _minPolygonArea, 0f, 1000f);
            EditorGUILayout.HelpBox("Discards small noisy polygons (e.g., from holes in the object) with area below this value in square pixels.", MessageType.None);

            _fillHoles = EditorGUILayout.Toggle("Fill Holes", _fillHoles);
            EditorGUILayout.HelpBox("If true, ignores inner cutouts (holes) caused by overlapping objects, resulting in one solid outer polygon.", MessageType.None);
        }

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

        if (!string.IsNullOrEmpty(_lastProcessedDatasetPath))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Last Conversion Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Dataset:", EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(_lastProcessedDatasetPath, EditorStyles.textField, GUILayout.Height(18));
            EditorGUILayout.LabelField($"Frames Converted: {_lastProcessedFrameCount}");
            EditorGUILayout.LabelField($"Labels After Filtering: {_lastFilteredLabelCount}");

            if (_lastClassCounts.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Class Occurrences After Filtering", EditorStyles.boldLabel);
                foreach (var pair in _lastClassCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key))
                {
                    string className = _lastYoloClassNames.TryGetValue(pair.Key, out var mappedName)
                        ? mappedName
                        : $"Class {pair.Key}";
                    EditorGUILayout.LabelField($"{pair.Key}: {className}", pair.Value.ToString());
                }
            }
        }

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

        // Create Output Folders (Compatible with organize_dataset.py)
        string yoloRoot = Path.Combine(datasetPath, "yolo_dataset");
        string yoloImgPath = Path.Combine(yoloRoot, "images");
        string yoloLblPath = Path.Combine(yoloRoot, "labels");

        if (Directory.Exists(yoloRoot))
        {
            Directory.Delete(yoloRoot, true); // Clean old generation
        }

        Directory.CreateDirectory(yoloImgPath);
        Directory.CreateDirectory(yoloLblPath);

        // Build ID Map
        Dictionary<int, int> idMap = new Dictionary<int, int>();
        Dictionary<int, string> yoloClassNames = new Dictionary<int, string>();
        string defPath = Path.Combine(datasetPath, "annotation_definitions.json");

        string targetDefId = _exportFormat == ExportFormat.BoundingBox ? "bounding box" : "instance segmentation";

        if (File.Exists(defPath))
        {
            JObject root = JObject.Parse(File.ReadAllText(defPath));
            var definitions = root["annotationDefinitions"] ?? root["annotation_definitions"];

            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    string defId = def["id"]?.ToString();
                    if (defId != null && (defId == targetDefId || defId == "bounding box 2d")) // Sometimes unity names it bounding box 2d
                    {
                        var spec = def["spec"];
                        int yoloIndex = 0;
                        foreach (var item in spec)
                        {
                            int soloId = item["label_id"].Value<int>();
                            idMap[soloId] = yoloIndex;
                            string labelName = item["label_name"]?.ToString() ?? $"Class {yoloIndex}";
                            yoloClassNames[yoloIndex] = labelName;
                            Debug.Log($"Mapping: {labelName} (SOLO ID {soloId}) → YOLO {yoloIndex}");
                            yoloIndex++;
                        }
                    }
                }
            }
        }

        if (idMap.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", $"No mapping found for '{targetDefId}' in annotation_definitions.json. Check your labelers.", "OK");
        }

        // Generate data.yaml (for compatibility if not using organize script)
        string yamlContent = $"path: {yoloRoot}\n" +
                             $"train: images\n" +
                             $"val: images\n\n" +
                             $"nc: {yoloClassNames.Count}\n" +
                             $"names: [";

        var sortedNames = yoloClassNames.OrderBy(kvp => kvp.Key).Select(kvp => $"'{kvp.Value}'").ToArray();
        yamlContent += string.Join(", ", sortedNames) + "]\n";

        File.WriteAllText(Path.Combine(yoloRoot, "data.yaml"), yamlContent);

        // Process Sequences
        int processedCount = 0;
        int filteredLabelCount = 0;
        Dictionary<int, int> classCounts = new Dictionary<int, int>();
        foreach (var seqDir in Directory.GetDirectories(datasetPath, "sequence.*"))
        {
            foreach (var jsonFile in Directory.GetFiles(seqDir, "step*.frame_data.json"))
            {
                ConvertFrame(jsonFile, yoloImgPath, yoloLblPath, idMap, classCounts, ref filteredLabelCount);
                processedCount++;
            }
        }

        _lastProcessedDatasetPath = datasetPath;
        _lastProcessedFrameCount = processedCount;
        _lastFilteredLabelCount = filteredLabelCount;
        _lastYoloClassNames = yoloClassNames;
        _lastClassCounts = classCounts;

        Debug.Log($"<color=green>Success!</color> Converted {processedCount} frames to YOLO format.");
        Debug.Log($"Dataset Root: {yoloRoot}");
        foreach (var pair in classCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key))
        {
            string className = yoloClassNames.TryGetValue(pair.Key, out var mappedName)
                ? mappedName
                : $"Class {pair.Key}";
            Debug.Log($"Class Count: {pair.Key} ({className}) = {pair.Value}");
        }

        EditorUtility.DisplayDialog("Conversion Complete",
            $"Converted {processedCount} frames.\n\nOutput:\n{yoloRoot}", "OK");
    }

    private void ConvertFrame(
        string jsonPath,
        string imgOutDir,
        string lblOutDir,
        Dictionary<int, int> idMap,
        Dictionary<int, int> classCounts,
        ref int filteredLabelCount)
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
                    var values = m["values"];
                    if (values != null)
                    {
                        foreach (var val in values)
                        {
                            visibility[val["instanceId"].Value<int>()] = val["percentVisible"].Value<float>();
                            // percentVisible is the tightest filter: it accounts for both frame presence AND occlusions.
                            // This ensures we only train on clear, unobstructed objects that are sufficiently visible.
                        }
                    }
                }
            }
        }

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

            string txtContent = "";
            var annotations = cap["annotations"];
            if (annotations != null)
            {
                if (_exportFormat == ExportFormat.BoundingBox)
                {
                    txtContent = ProcessBoundingBox(annotations, idMap, classCounts, visibility, ref filteredLabelCount);
                }
                else if (_exportFormat == ExportFormat.Segmentation)
                {
                    txtContent = ProcessSegmentation(annotations, Path.GetDirectoryName(jsonPath), idMap, classCounts, visibility, ref filteredLabelCount);
                }
            }

            string txtName = Path.GetFileNameWithoutExtension(uniqueName) + ".txt";
            File.WriteAllText(Path.Combine(lblOutDir, txtName), txtContent);
        }
    }

    private string ProcessBoundingBox(JToken annotations, Dictionary<int, int> idMap, Dictionary<int, int> classCounts, Dictionary<int, float> visibility, ref int filteredLabelCount)
    {
        string txtContent = "";
        foreach (var ann in annotations)
        {
            if (ann["@type"]?.ToString().Contains("BoundingBox2DAnnotation") == true)
            {
                var values = ann["values"];
                if (values != null)
                {
                    foreach (var val in values)
                    {
                        int instId = val["instanceId"].Value<int>();

                        if (visibility.TryGetValue(instId, out float vis) && vis < _minVisibility)
                            continue;

                        int labelId = val["labelId"].Value<int>();
                        if (!idMap.TryGetValue(labelId, out int yoloId)) continue;

                        float x = val["origin"][0].Value<float>();
                        float y = val["origin"][1].Value<float>();
                        float w = val["dimension"][0].Value<float>();
                        float h = val["dimension"][1].Value<float>();

                        float centerX = (x + (w / 2.0f)) / _resolution.x;
                        float centerY = (y + (h / 2.0f)) / _resolution.y;
                        float normW = w / _resolution.x;
                        float normH = h / _resolution.y;

                        txtContent += $"{yoloId} {centerX:F6} {centerY:F6} {normW:F6} {normH:F6}\n";
                        filteredLabelCount++;
                        classCounts[yoloId] = classCounts.TryGetValue(yoloId, out int count) ? count + 1 : 1;
                    }
                }
            }
        }
        return txtContent;
    }

    private string ProcessSegmentation(JToken annotations, string jsonDir, Dictionary<int, int> idMap, Dictionary<int, int> classCounts, Dictionary<int, float> visibility, ref int filteredLabelCount)
    {
        string txtContent = "";
        string maskFilename = null;
        Dictionary<int, Color32> instanceColors = new Dictionary<int, Color32>();
        Dictionary<int, int> instanceLabels = new Dictionary<int, int>();

        foreach (var ann in annotations)
        {
            if (ann["@type"]?.ToString().Contains("InstanceSegmentationAnnotation") == true)
            {
                maskFilename = ann["filename"]?.ToString();
                var instances = ann["instances"];
                if (instances != null)
                {
                    foreach (var inst in instances)
                    {
                        int instId = inst["instanceId"].Value<int>();
                        int labelId = inst["labelId"].Value<int>();
                        var colorArr = inst["color"];
                        if (colorArr != null && colorArr.Count() >= 4)
                        {
                            Color32 c = new Color32(
                                colorArr[0].Value<byte>(),
                                colorArr[1].Value<byte>(),
                                colorArr[2].Value<byte>(),
                                colorArr[3].Value<byte>()
                            );
                            instanceColors[instId] = c;
                            instanceLabels[instId] = labelId;
                        }
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(maskFilename)) return txtContent;

        string maskPath = Path.Combine(jsonDir, maskFilename);
        if (!File.Exists(maskPath)) return txtContent;

        // Load the mask texture
        Texture2D maskTex = new Texture2D(2, 2);
        maskTex.LoadImage(File.ReadAllBytes(maskPath));
        Color32[] pixels = maskTex.GetPixels32();
        int width = maskTex.width;
        int height = maskTex.height;

        foreach (var kvp in instanceColors)
        {
            int instId = kvp.Key;
            Color32 targetColor = kvp.Value;
            int labelId = instanceLabels[instId];

            if (!idMap.TryGetValue(labelId, out int yoloId)) continue;

            // Check visibility if available
            if (visibility.TryGetValue(instId, out float vis) && vis < _minVisibility)
                continue;

            var polygons = FindContours(pixels, width, height, targetColor, _fillHoles);

            foreach (var poly in polygons)
            {
                if (PolygonArea(poly) < _minPolygonArea) continue;

                var simplified = SimplifyDouglasPeucker(poly, _polygonSimplificationTolerance);
                if (simplified.Count < 3) continue;

                string line = $"{yoloId}";
                foreach (var pt in simplified)
                {
                    float normX = pt.x / (float)width;
                    float normY = 1.0f - (pt.y / (float)height); // Unity Texture2D Y is bottom-up, YOLO Y is top-down
                    line += $" {normX:F6} {normY:F6}";
                }
                txtContent += line + "\n";

                filteredLabelCount++;
                classCounts[yoloId] = classCounts.TryGetValue(yoloId, out int count) ? count + 1 : 1;
            }
        }

        Object.DestroyImmediate(maskTex); // Free memory
        return txtContent;
    }

    #region Contour Tracing

    private static readonly int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

    private static List<List<Vector2>> FindContours(Color32[] pixels, int width, int height, Color32 targetColor, bool fillHoles)
    {
        List<List<Vector2>> contours = new List<List<Vector2>>();
        bool[] visited = new bool[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                if (!visited[idx] && ColorsMatch(pixels[idx], targetColor))
                {
                    if (IsBoundary(pixels, width, height, x, y, targetColor))
                    {
                        var contour = Trace(pixels, width, height, x, y, targetColor, visited);
                        if (contour.Count > 2)
                        {
                            contours.Add(contour);
                            if (fillHoles)
                            {
                                FillPolygon(contour, visited, width, height);
                            }
                        }
                    }
                }
            }
        }
        return contours;
    }

    private static bool ColorsMatch(Color32 c1, Color32 c2)
    {
        // Ignore alpha for comparison, RGB should match exactly
        return c1.r == c2.r && c1.g == c2.g && c1.b == c2.b;
    }

    private static bool IsBoundary(Color32[] pixels, int width, int height, int x, int y, Color32 target)
    {
        if (x == 0 || x == width - 1 || y == 0 || y == height - 1) return true;
        if (!ColorsMatch(pixels[y * width + (x + 1)], target)) return true;
        if (!ColorsMatch(pixels[y * width + (x - 1)], target)) return true;
        if (!ColorsMatch(pixels[(y + 1) * width + x], target)) return true;
        if (!ColorsMatch(pixels[(y - 1) * width + x], target)) return true;
        return false;
    }

    private static List<Vector2> Trace(Color32[] pixels, int width, int height, int startX, int startY, Color32 target, bool[] visited)
    {
        List<Vector2> contour = new List<Vector2>();
        int currX = startX;
        int currY = startY;
        int dir = 7;

        int maxIters = width * height;
        int iter = 0;

        do
        {
            contour.Add(new Vector2(currX, currY));
            visited[currY * width + currX] = true;

            bool foundNext = false;
            int searchDir = (dir + 6) % 8; // Turn left 90 degrees

            for (int i = 0; i < 8; i++)
            {
                int d = (searchDir + i) % 8;
                int nx = currX + dx[d];
                int ny = currY + dy[d];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (ColorsMatch(pixels[ny * width + nx], target))
                    {
                        currX = nx;
                        currY = ny;
                        dir = d;
                        foundNext = true;
                        break;
                    }
                }
            }

            if (!foundNext) break;
            iter++;
        } while ((currX != startX || currY != startY) && iter < maxIters);

        return contour;
    }

    private static List<Vector2> SimplifyDouglasPeucker(List<Vector2> points, float epsilon)
    {
        if (points.Count < 3) return points;

        float dmax = 0;
        int index = 0;

        for (int i = 1; i < points.Count - 1; i++)
        {
            float d = PerpendicularDistance(points[i], points[0], points[points.Count - 1]);
            if (d > dmax)
            {
                index = i;
                dmax = d;
            }
        }

        if (dmax > epsilon)
        {
            var recResults1 = SimplifyDouglasPeucker(points.GetRange(0, index + 1), epsilon);
            var recResults2 = SimplifyDouglasPeucker(points.GetRange(index, points.Count - index), epsilon);

            var result = new List<Vector2>(recResults1);
            result.RemoveAt(result.Count - 1);
            result.AddRange(recResults2);
            return result;
        }
        else
        {
            return new List<Vector2> { points[0], points[points.Count - 1] };
        }
    }

    private static float PerpendicularDistance(Vector2 pt, Vector2 lineStart, Vector2 lineEnd)
    {
        float dx = lineEnd.x - lineStart.x;
        float dy = lineEnd.y - lineStart.y;

        if (dx == 0 && dy == 0) return Vector2.Distance(pt, lineStart);

        float t = ((pt.x - lineStart.x) * dx + (pt.y - lineStart.y) * dy) / (dx * dx + dy * dy);

        if (t < 0) return Vector2.Distance(pt, lineStart);
        else if (t > 1) return Vector2.Distance(pt, lineEnd);

        Vector2 closestPt = new Vector2(lineStart.x + t * dx, lineStart.y + t * dy);
        return Vector2.Distance(pt, closestPt);
    }

    private static float PolygonArea(List<Vector2> polygon)
    {
        if (polygon.Count < 3) return 0;

        float area = 0;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            area += (polygon[j].x + polygon[i].x) * (polygon[j].y - polygon[i].y);
            j = i;
        }
        return Mathf.Abs(area / 2.0f);
    }

    private static void FillPolygon(List<Vector2> contour, bool[] visited, int width, int height)
    {
        int minY = height;
        int maxY = -1;
        foreach (var p in contour)
        {
            if (p.y < minY) minY = (int)p.y;
            if (p.y > maxY) maxY = (int)p.y;
        }

        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(height - 1, maxY);

        for (int y = minY; y <= maxY; y++)
        {
            List<float> intersections = new List<float>();
            int j = contour.Count - 1;
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p1 = contour[j];
                Vector2 p2 = contour[i];

                if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                {
                    float x = p1.x + (y - p1.y) / (p2.y - p1.y) * (p2.x - p1.x);
                    intersections.Add(x);
                }
                j = i;
            }

            intersections.Sort();

            for (int i = 0; i < intersections.Count; i += 2)
            {
                if (i + 1 < intersections.Count)
                {
                    int startX = Mathf.Max(0, Mathf.CeilToInt(intersections[i]));
                    int endX = Mathf.Min(width - 1, Mathf.FloorToInt(intersections[i + 1]));
                    for (int x = startX; x <= endX; x++)
                    {
                        visited[y * width + x] = true;
                    }
                }
            }
        }
    }

    #endregion
}
