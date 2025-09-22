using System.Collections;
using System.Collections.Generic;
using Nanover.Frontend.XR;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using static SimulationInformationDisplay;

namespace NanoverImd.Interaction
{
    public class ReferenceLineManager : MonoBehaviour
    {
        [SerializeField] private LineManager lineManager;
        [SerializeField] private Transform pointerMesh; // 
        [SerializeField] private Transform RHSSimulationSpaceTransform;
        [SerializeField] private Transform propsManagerTransform;
        [SerializeField] private SimulationInformationDisplay simulationInformationDisplay;
        [SerializeField] private float singlePointThreshold = 0.05f;
        [SerializeField] private float snapshotFrequency = 0.01f;
        [SerializeField] private Transform userPointer; // The visual pointer 

        // Replace list of indices with list of timestamps
        private List<long> createdLineTimestamps = new();

        // Change from int to long for timestamp
        private long lastLineTimestamp = -1;
        private float lineLength = 0.0f;
        private float lineSmoothnessA = 0.0f;
        private double lineSmoothnessB = 0.0f;
        private float drawingElapsedTime = 0.0f;
        private Renderer pointerRenderer;
        private Nanover.Frontend.Input.IButton primaryButton, secondaryButton, menuButton, xButton, yButton;
        private bool primaryButtonPrevPressed, secondaryButtonPrevPressed, menuButtonPrevPressed, xButtonPrevPressed, yButtonPrevPressed;

        public void OnDisconnect()
        {
            lastLineTimestamp = -1;
            createdLineTimestamps.Clear();
        }

        void OnEnable()
        {
            primaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.primaryButton);
            secondaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.secondaryButton);
            menuButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.menuButton);
            xButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.primaryButton);
            yButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.secondaryButton);

            pointerRenderer = pointerMesh.gameObject.GetComponentInChildren<Renderer>();
            pointerRenderer.enabled = false;

            secondaryButton.Pressed += () =>
            {
                lineManager.RemoveAllLines(LineManager.DASH_LINE);
            };

            StartCoroutine(RefreshLines());
        }

        private IEnumerator RefreshLines()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                lineManager.SendDirtyLines();
            }
        }

        void updateUIInfo(LineRenderer l)
        {
            // Note: GetLineLength still uses int index. This will need to be updated in LineManager
            // For now, we'll find the line renderer directly
            lineLength = CalculateLineLength(l);
            lineSmoothnessA = LineManager.CalculateAngularSmoothness(l) / Mathf.PI;
            lineSmoothnessB = LineManager.CalculateSmoothness(l);

            simulationInformationDisplay.UpdateData(DataKeys.refLength, lineLength.ToString("F2") + "nm");
            simulationInformationDisplay.UpdateData(DataKeys.refJagger, (lineSmoothnessA * 100).ToString("F1"));
            simulationInformationDisplay.UpdateData(DataKeys.refTriplet, lineSmoothnessB.ToString("F2"));
            simulationInformationDisplay.UpdateData(DataKeys.refPoints, l.positionCount.ToString());
            simulationInformationDisplay.UpdateData(DataKeys.refOrigin, l.GetPosition(0).ToString("F1"));
            simulationInformationDisplay.UpdateData(DataKeys.refEnd, l.GetPosition(l.positionCount - 1).ToString("F1"));
            simulationInformationDisplay.RefreshDisplay();
        }

        // Helper method to calculate line length directly from LineRenderer
        private float CalculateLineLength(LineRenderer lineRenderer)
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return 0f;

            float length = 0f;
            for (int i = 0; i < lineRenderer.positionCount - 1; i++)
            {
                length += Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
            }
            return length;
        }

        // Helper method to get LineRenderer by timestamp
        private LineRenderer GetLineRendererByTimestamp(long timestamp)
        {
            if (timestamp < 0) return null;

            // get the line renderer directly from LineManager
            return lineManager.GetLineRenderer(timestamp);
        }


        private LineRenderer GetLastLineRenderer()
        {
            //if (createdLineTimestamps.Count == 0)
            //{
                // use the last ref line from the linemanager
                return lineManager.GetLastLineRenderer(LineManager.DASH_LINE);
            //}
            //long lastTimestamp = createdLineTimestamps[createdLineTimestamps.Count - 1];
            //return GetLineRendererByTimestamp(lastTimestamp);
        }



        void Update()
        {
            pointerMesh.position = userPointer.position;
            pointerMesh.rotation = Quaternion.LookRotation(userPointer.transform.forward, userPointer.transform.up);

            simulationInformationDisplay.UpdateData(DataKeys.rightPosition, pointerMesh.localPosition.ToString("F2"));

            if (Time.frameCount % 2 == 0)
            {
                var line = GetLastLineRenderer();
                if (line != null && line.positionCount > 0)
                {
                    updateUIInfo(line);
                }
            }

            // Draw line, point by point
            if (primaryButton.IsPressed)
            {
                // first time pressing the button
                if (!primaryButtonPrevPressed)
                {
                    UnityEngine.Debug.Log("Creating a new reference line");
                    lastLineTimestamp = lineManager.CreateNewLine(LineManager.DASH_LINE);
                    createdLineTimestamps.Add(lastLineTimestamp);
                    drawingElapsedTime = snapshotFrequency;
                    pointerRenderer.material.color = new UnityEngine.Color(1f, 1f, 1f, 0.5f);

                    AddReferencePoint();
                }

                // add points to the current line
                drawingElapsedTime += Time.deltaTime;
                if (drawingElapsedTime >= snapshotFrequency)
                {
                    drawingElapsedTime = 0.0f;
                    AddReferencePoint();
                }

                // drag the last point line if it just happened,
                // this is to reduce the number of points
                else
                {
                    DragLastPointOnLine();
                }
            }

            // when finishing drawing
            else if (primaryButtonPrevPressed && lastLineTimestamp >= 0)
            {
                var line = GetLineRendererByTimestamp(lastLineTimestamp);
                // Note: Simplify method may need to be updated in LineManager to use timestamp
                if (line != null)
                {
                    // For now we'll use the line renderer's built-in method
                    line.Simplify(0.01f);
                }
                pointerRenderer.material.color = new UnityEngine.Color(1f, 1f, 1f, 0.1f);
            }

            // move the reference prop
            if (xButton.IsPressed)
            {
                propsManagerTransform.localScale = RHSSimulationSpaceTransform.localScale * 0.1f;
                propsManagerTransform.localPosition = pointerMesh.localPosition;
                propsManagerTransform.localRotation = pointerMesh.localRotation;
            }

            // save previous button states
            primaryButtonPrevPressed = primaryButton.IsPressed;
            secondaryButtonPrevPressed = secondaryButton.IsPressed;
            xButtonPrevPressed = xButton.IsPressed;
            yButtonPrevPressed = yButton.IsPressed;
            menuButtonPrevPressed = menuButton.IsPressed;
        }

        private void AddReferencePoint()
        {
            if (lastLineTimestamp < 0) return;

            Vector3 pos = pointerMesh.localPosition;

            var line = GetLineRendererByTimestamp(lastLineTimestamp);

            if (line != null && line.positionCount >= 2)
            {
                if (Vector3.Distance(line.GetPosition(line.positionCount - 1), pos) < singlePointThreshold)
                    return;
            }

            lineManager.AddPointToLine(lastLineTimestamp, pos);
        }

        private void DragLastPointOnLine()
        {
            if (lastLineTimestamp < 0) return;
            Vector3 pos = pointerMesh.localPosition;
            lineManager.DragLastPoint(lastLineTimestamp, pos);
        }
    }
}