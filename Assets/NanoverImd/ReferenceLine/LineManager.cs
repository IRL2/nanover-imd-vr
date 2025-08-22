using System.Collections.Generic;
using System.Linq;
using NanoverImd;
using UnityEngine;
using UnityEngine.InputSystem;

public class LineManager : MonoBehaviour
{
    [SerializeField] private GameObject dashLinePrefab;
    [SerializeField] private GameObject solidLinePrefab;
    [SerializeField] private Transform simulationParent;

    [SerializeField] private NanoverImdSimulation simulation;

    private readonly List<LineData> lines = new(); // Stores LineData for each line
    private readonly HashSet<int> dirtyLines = new HashSet<int>();

    // types of lines
    public const int SOLID_LINE = 0;
    public const int DASH_LINE = 1;

    // Struct to store line type, renderer, and points

    private struct LineData
    {
        public int Type;
        public LineRenderer Renderer;
        public List<Vector3> Points;

        public LineData(int type, LineRenderer renderer)
        {
            Type = type;
            Renderer = renderer;
            Points = new List<Vector3>();
        }
    }


    public int CreateNewLine(int type)
    {
        var lineObj = Instantiate(type == DASH_LINE ? dashLinePrefab : solidLinePrefab, gameObject.transform);
        var lineRenderer = lineObj.GetComponent<LineRenderer>();
        lines.Add(new LineData(type, lineRenderer));
        lineRenderer.name = "line." + (lines.Count -1) + "." + (type == DASH_LINE ? "reference" : "trail");
        return lines.Count - 1;
    }

    public void AddPointToLine(int index, Vector3 point)
    {
        if (index < 0 || index >= lines.Count) return;
        var lineData = lines[index];
        lineData.Points.Add(point);
        dirtyLines.Add(index);

        lineData.Renderer.positionCount = lineData.Points.Count;
        for (var i = 0; i < lineData.Points.Count; i++)
            lineData.Renderer.SetPosition(i, lineData.Points[i]);
    }

    public void SendDirtyLines()
    {
        foreach (var index in dirtyLines)
            SendLine(index);
        dirtyLines.Clear();
    }

    public void SendLine(int index)
    {
        if (index < 0 || index >= lines.Count) return;
        var lineData = lines[index];
        var coords = Nanover.Core.Serialization.Serialization.ToDataStructure(lineData.Points);
        string key = "lines." + index + (lines[index].Type == DASH_LINE ? ".reference" : ".trail");
        Debug.Log($"Sending line {index} with key {key}");
        simulation.Multiplayer.SetSharedState(key, coords);
    }

    public void DragLastPoint(int index, Vector3 point)
    {
        if (index < 0 || index >= lines.Count) return;
        var lineData = lines[index];
        if (lineData.Points.Count == 0) return;
        lineData.Points[^1] = point;
        lineData.Renderer.SetPosition(lineData.Points.Count - 1, point);
        lines[index] = lineData;
    }

    public void ResetLine(int index)
    {
        if (index < 0 || index >= lines.Count) return;
        var lineData = lines[index];
        lineData.Points.Clear();
        lineData.Renderer.positionCount = 0;
        lines[index] = lineData;
    }

    public void RemoveLine(int index)
    {
        Debug.Log($"Attempting to remove line {index}");

        if (index < 0 || index >= lines.Count) return;

        var lineData = lines[index];

        if (lineData.Renderer == null)
        {
            Debug.LogWarning($"Line at index {index} is null, cannot remove.");
            return;
        }

        string key = "lines." + index + (lines[index].Type == DASH_LINE ? ".reference" : ".trail");
        Debug.Log($"Attempting to remove key {key}");
        simulation.Multiplayer.RemoveSharedStateKey(key);

        if (lineData.Renderer != null && lineData.Renderer.gameObject != null)
        {
            Destroy(lineData.Renderer.gameObject);
            lines.RemoveAt(index);
        }
    }

    // search for the latest line of type 'type' and remove it from the array
    public void UndoLine(int type)
    {
        Debug.Log($"Attempting to undo line of type {type}");

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i].Type == type)
            {
                RemoveLine(i);
                return;
            }
        }
    }

    public void RemoveAllLines()
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            RemoveLine(i);
        }
        lines.Clear();
    }

    public float GetLineLength(int index)
    {
        if (index < 0 || index >= lines.Count) return 0f;
        float length = 0f;
        var points = lines[index].Points;
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

    public LineRenderer GetLineRenderer(int index)
    {
        if (index < 0 || index >= lines.Count) return null;
        return lines[index].Renderer;
    }

    public void SetLineColor(int index, UnityEngine.Color color)
    {
        if (index < 0 || index >= lines.Count) return;
        lines[index].Renderer.startColor = color;
        lines[index].Renderer.endColor = color;
    }

    public void ScaleLineWeight(int index, float scale)
    {
        if (index < 0 || index >= lines.Count) return;
        lines[index].Renderer.startWidth *= scale;
        lines[index].Renderer.endWidth *= scale;
    }

    /// <summary>
    /// Given a line index, simplifies the line using the Douglas-Peucker algorithm.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="tolerance"></param>
    public void SimplifyLine(int index, float? tolerance = 0.001f)
    {
        if (index < 0 || index >= lines.Count) return;
        lines[index].Renderer.Simplify((float)tolerance);

        lines[index].Renderer.GetPositions(lines[index].Points.ToArray());
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


    public void RestoreLines()
    {
        Dictionary<string, object> stateDictionary = simulation.Multiplayer.SharedStateDictionary;

        // sort by alphabetical order, so that lines are restored in the correct order
        stateDictionary = stateDictionary.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // filter entries that start with "lines."
        var stateLines = stateDictionary.Where(kvp => kvp.Key.StartsWith("lines.")).ToList();

        // no lines to restore
        if (stateLines.Count == 0)
        {
            Debug.Log("No lines to restore.");
            while (transform.childCount > 0) Destroy(transform.GetChild(0).gameObject);
            lines.Clear();
            return;
        }

        // get local existing lines in the scene
        var existingLines = new Dictionary<string, GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("line."))
            {
                existingLines[child.name] = child.gameObject;
            }
        }

        // iterate over the lines in the state, 
        // if a line exist locally: try to update it
        // else: create it
        foreach (var kvp in stateLines)
        {
            string name = kvp.Key.Remove(4,1); // remove the "s" in "lines", because local lines are named "line.x.type" // this should be fixed
            //Debug.Log("Looking for existent line: " + name + " of " + kvp.Key);

            var pointsList = Nanover.Core.Serialization.Serialization.FromDataStructure<List<Vector3>>(kvp.Value);

            if (existingLines.TryGetValue(name, out GameObject existingLineObj))
            {
                // if it already exist, update it or skip it
                if (existingLineObj.GetComponent<LineRenderer>().positionCount != pointsList.Count)
                {
                    Debug.Log($"Found existing line for index {name} with different points count");
                    UpdateLineData(int.Parse(kvp.Key.Split('.')[1]), pointsList);
                }
            }
            else
            {
                Debug.Log($"No existing line found for {name}, creating a new one");
                CreateLineFromPoints(kvp.Key, pointsList);
            }
        }

    }


    private void CreateLineFromPoints(string key, List<Vector3> pointsList)
    {
        string[] parts = key.Split('.');
        if (parts.Length < 3) return;

        int entryIndex = int.Parse(parts[1]);
        int type = parts[2] == "reference" ? DASH_LINE : SOLID_LINE;

        int newLineIndex = CreateNewLine(type);

        //Debug.Log($"Restoring line {key} of type {type} on {lines[newLineIndex].Renderer.transform.parent.name}");

        lines[newLineIndex].Points.AddRange(pointsList);
        lines[newLineIndex].Renderer.positionCount = pointsList.Count;
        lines[newLineIndex].Renderer.SetPositions(pointsList.ToArray());
    }


    private void UpdateLineData(int index, List<Vector3> pointsList)
    {
        // update the points list and renderer
        lines[index].Points.Clear();
        lines[index].Points.AddRange(pointsList);
        lines[index].Renderer.positionCount = pointsList.Count;
        lines[index].Renderer.SetPositions(pointsList.ToArray());
    }

}