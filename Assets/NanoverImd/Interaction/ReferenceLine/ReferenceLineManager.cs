using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

using Nanover.Frontend.XR;

using TMPro;
using System.Collections;

namespace NanoverImd.Interaction
{
    public class ReferenceLineManager : MonoBehaviour
    {
        [SerializeField] private LineManager lineManager;
        [SerializeField] private Transform pointerMesh; // 
        [SerializeField] private Transform RHSSimulationSpaceTransform;
        [SerializeField] private Transform propsManagerTransform;
        //[SerializeField] private TextMeshPro lineInfoLabel;
        [SerializeField] private SimulationInformationDisplay simulationInformationDisplay;
        [SerializeField] private float singlePointThreshold = 0.05f;
        [SerializeField] private float snapshotFrequency = 0.01f;
        [SerializeField] private Transform userPointer; // The visual pointer 

        private List<int> createdLineIndices = new();

        private int currentLineIndex = -1;
        private float lineLength = 0.0f;
        private float lineSmoothnessA = 0.0f;
        private double lineSmoothnessB = 0.0f;
        private float drawingElapsedTime = 0.0f;
        private Renderer pointerRenderer;
        //private bool modeActive = false;
        private Nanover.Frontend.Input.IButton primaryButton, secondaryButton, menuButton, xButton, yButton;
        private bool primaryButtonPrevPressed, secondaryButtonPrevPressed, menuButtonPrevPressed, xButtonPrevPressed, yButtonPrevPressed;

        public void OnDisconnect()
        {
            currentLineIndex = -1;
            createdLineIndices.Clear();
        }

        void OnEnable()
        {
            primaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.primaryButton);
            secondaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.secondaryButton);
            menuButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.menuButton);
            xButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.primaryButton);
            yButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.secondaryButton);

            //lineInfoLabel.text = "";
            pointerRenderer = pointerMesh.gameObject.GetComponentInChildren<Renderer>();
            pointerRenderer.enabled = false;

            secondaryButton.Pressed += () =>
            {
                lineManager.UndoLine(LineManager.DASH_LINE);
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
            lineLength = lineManager.GetLineLength(currentLineIndex);
            lineSmoothnessA = LineManager.CalculateAngularSmoothness(l) / Mathf.PI;
            lineSmoothnessB = LineManager.CalculateSmoothness(l);

            simulationInformationDisplay.UpdateData("refLength", lineLength.ToString("F2"));
            simulationInformationDisplay.UpdateData("refJagger", (lineSmoothnessA * 100).ToString("F1"));
            simulationInformationDisplay.UpdateData("refTriplet", lineSmoothnessB.ToString("F2"));
            simulationInformationDisplay.UpdateData("refPoints", l.positionCount.ToString());
            simulationInformationDisplay.UpdateData("refOrigin", l.GetPosition(0).ToString("F2"));
            simulationInformationDisplay.UpdateData("refEnd", l.GetPosition(l.positionCount - 1).ToString("F2"));
            simulationInformationDisplay.RefreshDisplay();
        }

        void Update()
        {
            pointerMesh.position = userPointer.position;
            pointerMesh.rotation = Quaternion.LookRotation(userPointer.transform.forward, userPointer.transform.up);

            //lineInfoLabel.text = "\npointer at " + pointerMesh.localPosition.ToString() + " \n";
            simulationInformationDisplay.UpdateData("pointerPosition", pointerMesh.localPosition.ToString("F2"));

            if (currentLineIndex >= 0)
            {
                var line = lineManager.GetLineRenderer(currentLineIndex);
                if (line != null && line.positionCount > 0)
                {
                    lineLength = lineManager.GetLineLength(currentLineIndex);
                    lineSmoothnessA = LineManager.CalculateAngularSmoothness(line) / Mathf.PI;
                    lineSmoothnessB = LineManager.CalculateSmoothness(line);
                    updateUIInfo(line);
                }
            }

            // Draw
            if (primaryButton.IsPressed)
            {
                if (!primaryButtonPrevPressed)
                {
                    UnityEngine.Debug.Log("Creating a new reference line");
                    currentLineIndex = lineManager.CreateNewLine(LineManager.DASH_LINE);
                    createdLineIndices.Add(currentLineIndex);
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
            else if (primaryButtonPrevPressed && currentLineIndex >= 0)
            {
                var line = lineManager.GetLineRenderer(currentLineIndex);
                if (line != null) line.Simplify(0.01f);
                pointerRenderer.material.color = new UnityEngine.Color(1f, 1f, 1f, 0.1f);
                //line.widthMultiplier = 0.9f;
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
            if (currentLineIndex < 0) return;

            Vector3 pos = pointerMesh.localPosition;

            var line = lineManager.GetLineRenderer(currentLineIndex);

            if (line != null && line.positionCount >= 2)
            {
                if (Vector3.Distance(line.GetPosition(line.positionCount - 1), pos) < singlePointThreshold)
                    return;
            }

            lineManager.AddPointToLine(currentLineIndex, pos);

            //if (rightHandDevice.isValid)
            //    rightHandDevice.SendHapticImpulse(0u, 0.05f, 0.005f);
        }

        private void DragLastPointOnLine()
        {
            if (currentLineIndex < 0) return;
            Vector3 pos = pointerMesh.localPosition;
            lineManager.DragLastPoint(currentLineIndex, pos);
        }
    }
}