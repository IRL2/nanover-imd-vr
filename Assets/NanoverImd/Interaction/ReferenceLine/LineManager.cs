using System.Collections.Generic;
using System.Linq;
using NanoverImd;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class LineManager : MonoBehaviour
{
    [SerializeField] private GameObject dashLinePrefab;
    [SerializeField] private GameObject solidLinePrefab;
    [SerializeField] private Transform simulationParent;

    [SerializeField] private NanoverImdSimulation simulation;

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

    private readonly List<LineData> lines = new(); // Stores LineData for each line

    public int CreateNewLine(int type)
    {
        var lineObj = Instantiate(type == DASH_LINE ? dashLinePrefab : solidLinePrefab, gameObject.transform);
        var lineRenderer = lineObj.GetComponent<LineRenderer>();
        lines.Add(new LineData(type, lineRenderer));
        return lines.Count - 1;
    }

    public void AddPointToLine(int index, Vector3 point)
    {
        if (index < 0 || index >= lines.Count) return;
        var lineData = lines[index];
        lineData.Points.Add(point);
        lineData.Renderer.positionCount = lineData.Points.Count;
        lineData.Renderer.SetPositions(lineData.Points.ToArray());

        var coords = Nanover.Core.Serialization.Serialization.ToDataStructure(lineData.Points);
        string key = "lines." + index + (lines[index].Type == DASH_LINE ? ".reference" : ".trail");
        simulation.Multiplayer.SetSharedState(key, coords);

        lines[index] = lineData; // Structs are value types, so re-assign
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

    public void SimplifyLine(int index, float? tolerance = 0.001f)
    {
        if (index < 0 || index >= lines.Count) return;
        lines[index].Renderer.Simplify((float)tolerance);

        lines[index].Renderer.GetPositions(lines[index].Points.ToArray());

        //var simplifiedPoints = DouglasPeucker(lines[index].Points, tolerance);
        //lines[index].Points = simplifiedPoints;
        //lines[index].Renderer.positionCount = simplifiedPoints.Count;
        //lines[index].Renderer.SetPositions(simplifiedPoints.ToArray());
    }
}