using System.Collections.Generic;
using System.Linq;
using NanoverImd;
using UnityEngine;
using UnityEngine.InputSystem;
using static SimulationInformationDisplay;

public class LineManager : MonoBehaviour
{
    [SerializeField] private GameObject dashLinePrefab;
    [SerializeField] private GameObject solidLinePrefab;
    [SerializeField] private Transform simulationParent;

    [SerializeField] private NanoverImdSimulation simulation;

    // Replace List with Dictionary for timestamp-based keys
    private readonly Dictionary<long, LineData> lines = new();
    private readonly HashSet<long> dirtyLines = new();

    //[SerializeField] SimulationInformationDisplay simulationInformationDisplay;

    // types of lines
    public enum LineType
    {
        TRAIL = 0,
        REFERENCE = 1
    }

    public const int SOLID_LINE = 0;
    public const int DASH_LINE = 1;

    private float currentColorHue = 0.5f;


    // Struct to store line type, renderer, and points

    private struct LineData
    {
        public int Type;
        public LineRenderer Renderer;
        public List<Vector3> Points;
        public long Timestamp;

        public LineData(int type, LineRenderer renderer, long timestamp)
        {
            Type = type;
            Renderer = renderer;
            Points = new List<Vector3>();
            Timestamp = timestamp;
        }
    }

    private void Awake()
    {
        simulation.Multiplayer.SharedStateDictionaryKeyUpdated += (key, value) =>
        {
            if (key.StartsWith("lines."))
            {
                var line = FindLineDataByKey(key);
                var pointsList = Nanover.Core.Serialization.Serialization.FromDataStructure<List<Vector3>>(value);
                UpdateLineData(line.Timestamp, pointsList);
            }
        };

        LineData FindLineDataByKey(string key) => lines.Values.FirstOrDefault(data => data.Renderer.name == key);
    }

    // Create a new line and use a timestamp as its key
    public long CreateNewLine(int type)
    {
        var timestamp = System.DateTime.UtcNow.Ticks;
        var lineObj = Instantiate(type == DASH_LINE ? dashLinePrefab : solidLinePrefab, gameObject.transform);
        var lineRenderer = lineObj.GetComponent<LineRenderer>();
        lines[timestamp] = new LineData(type, lineRenderer, timestamp);
        lineRenderer.name = $"lines.{timestamp}.{(type == DASH_LINE ? "reference" : "trail")}";
        Debug.Log($"Creating line with timestamp {timestamp}");

        if (type == SOLID_LINE)
        {
            currentColorHue = (currentColorHue + 0.1f) % 1.0f;
            SetLineColor(timestamp, Color.HSVToRGB(currentColorHue, 0.85f, 0.85f));
        }

        //simulationInformationDisplay.UpdateData(DataKeys.numTrailLines, lines.Count.ToString());

        return timestamp;
    }

    public void AddPointToLine(long timestamp, Vector3 point)
    {
        //Debug.Log($"LineManager::AddPointToLine::Adding point to line with timestamp {timestamp}: {point}");
        if (!lines.ContainsKey(timestamp)) return;

        //var lineData = lines[timestamp];

        // add the point to the data structure
        lines[timestamp].Points.Add(point);

        lines[timestamp].Renderer.positionCount = lines[timestamp].Points.Count;
        for (var i = 0; i < lines[timestamp].Points.Count; i++)
            lines[timestamp].Renderer.SetPosition(i, lines[timestamp].Points[i]);

        // simplify the line & update the points
        SimplifyLine(timestamp, 0.002f);

        // mark this line as dirty for sending updates later
        dirtyLines.Add(timestamp);
    }

    public void SendDirtyLines()
    {
        foreach (var timestamp in dirtyLines)
            SendLine(timestamp);
        dirtyLines.Clear();
    }

    public void SendLine(long timestamp)
    {
        if (!lines.ContainsKey(timestamp)) return;
        var lineData = lines[timestamp];
        var coords = Nanover.Core.Serialization.Serialization.ToDataStructure(lineData.Points);
        string key = $"lines.{timestamp}{(lineData.Type == DASH_LINE ? ".reference" : ".trail")}";
        simulation.Multiplayer.SetSharedState(key, coords);
    }

    public void DragLastPoint(long timestamp, Vector3 point)
    {
        if (!lines.ContainsKey(timestamp)) return;
        var lineData = lines[timestamp];
        if (lineData.Points.Count == 0) return;
        lineData.Points[^1] = point;
        lineData.Renderer.SetPosition(lineData.Points.Count - 1, point);
        lines[timestamp] = lineData;
    }

    public void ResetLine(long timestamp)
    {
        if (!lines.ContainsKey(timestamp)) return;
        var lineData = lines[timestamp];
        lineData.Points.Clear();
        lineData.Renderer.positionCount = 0;
        lines[timestamp] = lineData;
    }

    public void RemoveLine(long timestamp)
    {
        Debug.Log($"Attempting to remove line with timestamp {timestamp}");

        if (!lines.ContainsKey(timestamp)) return;

        var lineData = lines[timestamp];

        if (lineData.Renderer == null)
        {
            Debug.LogWarning($"Line with timestamp {timestamp} is null, cannot remove.");
            return;
        }

        string key = $"lines.{timestamp}{(lineData.Type == DASH_LINE ? ".reference" : ".trail")}";
        Debug.Log($"Attempting to remove key {key}");
        simulation.Multiplayer.RemoveSharedStateKey(key);

        if (lineData.Renderer != null && lineData.Renderer.gameObject != null)
        {
            Destroy(lineData.Renderer.gameObject);
            lines.Remove(timestamp);
        }
    }

    // search for the latest line of type 'type' and remove it from the array
    public void UndoLine(int type)
    {
        Debug.Log($"Attempting to undo line of type {type}");

        if (lines.Count == 0) return;
        
        // Find the most recent line of the specified type
        long latestTimestamp = -1;
        foreach (var kvp in lines)
        {
            if (kvp.Value.Type == type && kvp.Key > latestTimestamp)
            {
                latestTimestamp = kvp.Key;
            }
        }
        
        if (latestTimestamp != -1)
        {
            RemoveLine(latestTimestamp);
        }
    }

    public void RemoveAllLines()
    {
        // Create a copy of keys to avoid modifying the collection during iteration
        var linesToRemove = lines.Keys.ToList();
        
        foreach (var timestamp in linesToRemove)
        {
            Debug.Log($"Will remove line {timestamp} of type {lines[timestamp].Type}");
            RemoveLine(timestamp);
        }
        
        // The lines dictionary should be empty now after all removals
        // but let's make sure
        lines.Clear();

        Dictionary<string, object> stateDictionary = simulation.Multiplayer.SharedStateDictionary;
        var stateLines = stateDictionary.Where(kvp => kvp.Key.StartsWith("lines.")).ToList();
        foreach (var kvp in stateLines)
        {
            simulation.Multiplayer.RemoveSharedStateKey(kvp.Key);
        }
    }

    public void RemoveAllLines(int type)
    {
        // Create a copy of keys where type matches
        var linesToRemove = lines.Where(l => l.Value.Type == type)
                            .Select(l => l.Key)
                            .ToList();
        
        foreach (var timestamp in linesToRemove)
        {
            Debug.Log($"Will remove line {timestamp} of type {lines[timestamp].Type}");
            RemoveLine(timestamp);
        }

        Dictionary<string, object> stateDictionary = simulation.Multiplayer.SharedStateDictionary;
        var stateLines = stateDictionary.Where(kvp => kvp.Key.StartsWith("lines.")).ToList();
        foreach (var kvp in stateLines)
        {
            if ((type == DASH_LINE && kvp.Key.EndsWith(".reference")) || (type == SOLID_LINE && kvp.Key.EndsWith(".trail")))
            {
                simulation.Multiplayer.RemoveSharedStateKey(kvp.Key);
            }
        }
    }

    public float GetLineLength(long timestamp)
    {
        if (!lines.ContainsKey(timestamp)) return 0f;
        float length = 0f;
        var points = lines[timestamp].Points;
        for (int i = 0; i < points.Count - 1; i++)
            length += Vector3.Distance(points[i], points[i + 1]);
        return length;
    }

    public static float CalculateSmoothness(LineRenderer lineRenderer)
    {
        int pointCount = lineRenderer.positionCount;
        if (pointCount < 3) return 0f;
        Vector3[] positions = new Vector3[pointCount];
        lineRenderer.GetPositions(positions);
        float sum = 0f;
        for (int i = 0; i < pointCount - 2; i++)
        {
            Vector3 secondDiff = positions[i + 2] - 2 * positions[i + 1] + positions[i];
            sum += secondDiff.sqrMagnitude;
        }
        return sum;
    }

    public static float CalculateAngularSmoothness(LineRenderer lineRenderer)
    {
        int pointCount = lineRenderer.positionCount;
        if (pointCount < 3) return 0f;
        Vector3[] positions = new Vector3[pointCount];
        lineRenderer.GetPositions(positions);
        float totalDeviation = 0f;
        int angleCount = 0;
        for (int i = 1; i < pointCount - 1; i++)
        {
            Vector3 prev = positions[i] - positions[i - 1];
            Vector3 next = positions[i + 1] - positions[i];
            if (prev.sqrMagnitude == 0f || next.sqrMagnitude == 0f) continue;
            float dot = Vector3.Dot(prev.normalized, next.normalized);
            dot = Mathf.Clamp(dot, -1f, 1f);
            float angle = Mathf.Acos(dot);
            float deviation = Mathf.Abs(Mathf.PI - angle);
            totalDeviation += deviation;
            angleCount++;
        }
        return (angleCount > 0) ? totalDeviation / angleCount : 0f;
    }

    public LineRenderer GetLineRenderer(long timestamp)
    {
        if (!lines.ContainsKey(timestamp)) return null;
        return lines[timestamp].Renderer;
    }

    public void SetLineColor(long timestamp, UnityEngine.Color color)
    {
        if (!lines.ContainsKey(timestamp)) return;
        lines[timestamp].Renderer.startColor = color;
        lines[timestamp].Renderer.endColor = color;
    }

    public void ScaleLineWeight(long timestamp, float scale)
    {
        if (!lines.ContainsKey(timestamp)) return;
        lines[timestamp].Renderer.startWidth *= scale;
        lines[timestamp].Renderer.endWidth *= scale;
    }

    /// <summary>
    /// Given a line index, simplifies the line using the LineRenderer-included Douglas-Peucker's algorithm.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="tolerance"></param>
    public void SimplifyLine(long timestamp, float? tolerance = 0.001f)
    {
        if (!lines.ContainsKey(timestamp)) return;

        if (lines[timestamp].Renderer.positionCount < 10) return;

        //Debug.Log($"LineManager::SimplifyLine::Simplifying line with timestamp {timestamp} using tolerance {tolerance}, from {lines[timestamp].Renderer.positionCount}, to {lines[timestamp].Points.Count}");

        lines[timestamp].Renderer.Simplify((float)tolerance);

        // update the points list to match the simplified line
        lines[timestamp].Points.Clear();
        for (var i = 0; i < lines[timestamp].Renderer.positionCount; i++)
            lines[timestamp].Points.Add(lines[timestamp].Renderer.GetPosition(i));
    }


    void OnEnable()
    {
        RestoreLines();
    }

    private void Update()
    {
        // check every second if there are lines to restore
        if (Time.frameCount % 120 == 0 && dirtyLines.Count() == 0)
        {
            RestoreLines();
        }
    }


    public void GetAmountOfLines(out int numRefLines, out int numTrailLines)
    {
        numRefLines = lines.Count(l => l.Value.Type == DASH_LINE);
        numTrailLines = lines.Count(l => l.Value.Type == SOLID_LINE);
    }


    public void RestoreLines()
    {
        Dictionary<string, object> stateDictionary = simulation.Multiplayer.SharedStateDictionary;

        // Filter entries that start with "lines."
        var stateLines = stateDictionary.Where(kvp => kvp.Key.StartsWith("lines.")).ToList();

        // No lines to restore
        if (stateLines.Count == 0)
        {
            //Debug.Log("No lines to restore.");
            while (transform.childCount > 0) Destroy(transform.GetChild(0).gameObject);
            lines.Clear();
            return;
        }

        // First, clean up any local lines that aren't in the shared state
        var sharedLineKeys = stateLines.Select(kvp => 
        {
            string[] parts = kvp.Key.Split('.');
            return parts.Length >= 2 ? parts[1] : null;
        }).Where(key => key != null).ToHashSet();

        // Create a list of timestamps to remove (lines that exist locally but not in shared state)
        var localLinesToRemove = new List<long>();
        foreach (var timestamp in lines.Keys)
        {
            if (!sharedLineKeys.Contains(timestamp.ToString()))
                localLinesToRemove.Add(timestamp);
        }

        // Remove local lines that don't exist in shared state
        foreach (var timestamp in localLinesToRemove)
        {
            Debug.Log($"Removing local line {timestamp} that's not in shared state");
            if (lines[timestamp].Renderer != null && lines[timestamp].Renderer.gameObject != null)
                Destroy(lines[timestamp].Renderer.gameObject);
            lines.Remove(timestamp);
        }

        // Now process lines from the shared state
        foreach (var kvp in stateLines)
        {
            string[] parts = kvp.Key.Split('.');
            if (parts.Length < 3) continue;
            
            // Extract timestamp from the key
            if (!long.TryParse(parts[1], out long timestamp))
            {
                Debug.LogError($"Failed to parse timestamp from key: {kvp.Key}");
                continue;
            }
            
            var pointsList = Nanover.Core.Serialization.Serialization.FromDataStructure<List<Vector3>>(kvp.Value);
            
            // Check if we already have this line locally
            if (lines.ContainsKey(timestamp))
            {
                // Update existing line if the point count differs
                var lineData = lines[timestamp];
                if (lineData.Renderer.positionCount != pointsList.Count)
                {
                    Debug.Log($"Updating existing line with timestamp {timestamp}");
                    UpdateLineData(timestamp, pointsList);
                }
            }
            else
            {
                // Create new line with the same timestamp as in the shared state
                Debug.Log($"Creating new line with timestamp {timestamp} from shared state");
                int type = parts[2].EndsWith("reference") ? DASH_LINE : SOLID_LINE;
                
                // Create line GameObject
                var lineObj = Instantiate(type == DASH_LINE ? dashLinePrefab : solidLinePrefab, gameObject.transform);
                var lineRenderer = lineObj.GetComponent<LineRenderer>();
                
                // Set up line data with the timestamp from shared state
                lines[timestamp] = new LineData(type, lineRenderer, timestamp);
                lineRenderer.name = $"lines.{timestamp}.{(type == DASH_LINE ? "reference" : "trail")}";

                // Apply line color for SOLID_LINE types
                if (type == SOLID_LINE)
                {
                    currentColorHue = (currentColorHue + 0.1f) % 1.0f;
                    SetLineColor(timestamp, Color.HSVToRGB(currentColorHue, 0.85f, 0.85f));
                }
                
                // Set the points
                UpdateLineData(timestamp, pointsList);
            }
        }
    }


    private void CreateLineFromPoints(string key, List<Vector3> pointsList)
    {
        string[] parts = key.Split('.');
        if (parts.Length < 3) return;

        // Extract timestamp from the key
        long timestamp;
        if (!long.TryParse(parts[1], out timestamp))
        {
            Debug.LogError($"Failed to parse timestamp from key: {key}");
            return;
        }
        
        int type = parts[2] == "reference" ? DASH_LINE : SOLID_LINE;

        long newLineTimestamp = CreateNewLine(type);

        Debug.Log($"Restoring line {key} of type {type} with timestamp {newLineTimestamp}");

        lines[newLineTimestamp].Points.AddRange(pointsList);
        lines[newLineTimestamp].Renderer.positionCount = pointsList.Count;
        lines[newLineTimestamp].Renderer.SetPositions(pointsList.ToArray());
    }

    private void UpdateLineData(long timestamp, List<Vector3> pointsList)
    {
        // update the points list and renderer
        if (!lines.ContainsKey(timestamp))
        {
            Debug.LogError($"Attempted to update non-existent line with timestamp {timestamp}");
            return;
        }
        
        lines[timestamp].Points.Clear();
        lines[timestamp].Points.AddRange(pointsList);
        lines[timestamp].Renderer.positionCount = pointsList.Count;
        lines[timestamp].Renderer.SetPositions(pointsList.ToArray());
    }

    public LineRenderer GetLastLineRenderer(int type)
    {
        var lastLine = lines.Where(l => l.Value.Type == type)
                           .OrderByDescending(l => l.Key)
                           .FirstOrDefault();
        return lastLine.Value.Renderer;
    }
}