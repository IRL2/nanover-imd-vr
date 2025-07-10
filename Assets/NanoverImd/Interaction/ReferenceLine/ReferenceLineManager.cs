using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nanover.Visualisation;
using NanoverImd;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
//using UnityEngine.XR.Interaction.Toolkit;

using Nanover.Frontend.Input;
using Nanover.Frontend.XR;

using TMPro;
using Nanover.Frontend.Controllers;
using System.Drawing;
using UnityEngine.UIElements;
using System.Reflection.Emit;

namespace NanoverImd.Interaction
{
    public class ReferenceLineManager : MonoBehaviour
    {
        [SerializeField] private LineManager lineManager;
        [SerializeField] private Transform pointerMesh; // 
        [SerializeField] private Transform RHSSimulationSpaceTransform;
        [SerializeField] private Transform propsManagerTransform;
        [SerializeField] private TextMeshPro lineInfoLabel;
        [SerializeField] private float singlePointThreshold = 0.05f;
        [SerializeField] private float snapshotFrequency = 0.01f;
        [SerializeField] private Transform userPointer; // The pointer that the user is using to draw lines, where the actual coordinates are taken from.

        private List<int> createdLineIndices = new();

        private int currentLineIndex = -1;
        private float lineLength = 0.0f;
        private float lineSmoothnessA = 0.0f;
        private double lineSmoothnessB = 0.0f;
        private float drawingElapsedTime = 0.0f;
        private Renderer pointerRenderer;
        private bool modeActive = false;
        private Nanover.Frontend.Input.IButton primaryButton, secondaryButton, menuButton, xButton, yButton;
        private bool primaryButtonPrevPressed, secondaryButtonPrevPressed, menuButtonPrevPressed, xButtonPrevPressed, yButtonPrevPressed;
        private InputDevice rightHandDevice;
        private UnityEngine.XR.HapticCapabilities hapticCapabilities;

        const string DRAWING_DISABLED = "<b>Press [menu] to enable draw mode";
        const string DRAWING_INSTRUCTIONS = "<b>Hold [A]</b> to draw a line\r\n<b>Press [A]</b> to add points to the line\r\n<b>Press [B]</b> to delete the line\r\n\r\n<b>Press [Y]</b> to reset trail\r\n<b>Press [X]</b> to position destiny\r\n\r\n<b>Press [menu]</b> to disable drawing mode";

        void Start()
        {
            primaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.primaryButton);
            secondaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.secondaryButton);
            menuButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.menuButton);
            xButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.primaryButton);
            yButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.secondaryButton);

            lineInfoLabel.text = "";
            pointerRenderer = pointerMesh.gameObject.GetComponentInChildren<Renderer>();
            pointerRenderer.enabled = false;
        }

        private void Awake()
        {
            TryToGetPointer();
            TryToEnableHaptics();
        }

        void updateUIInfo(LineRenderer l)
        {
            lineLength = lineManager.GetLineLength(currentLineIndex);
            lineSmoothnessA = LineManager.CalculateAngularSmoothness(l) / Mathf.PI;
            lineSmoothnessB = LineManager.CalculateSmoothness(l);
            lineInfoLabel.text += $"\n<u>trajectory reference line</u>{(primaryButton.IsPressed ? " [drawing] " : "")}";
            lineInfoLabel.text += $"\n   lenght is {lineLength:F2} nm";
            lineInfoLabel.text += $"\n   from {l.GetPosition(0):F2}";
            lineInfoLabel.text += $"\n   to {l.GetPosition(l.positionCount - 1):F2}";
            lineInfoLabel.text += $"\n   having {l.positionCount} points";
            lineInfoLabel.text += $"\n   angular triplets {(lineSmoothnessA * 100):F1}%";
            lineInfoLabel.text += $"\n   path jagger {lineSmoothnessB:F2}\n";
        }

        void Update()
        {
            if (userPointer == null)
            {
                TryToGetPointer();
                //TryToEnableHaptics();
                return;
            }

            // Delete the last line
            if (secondaryButton.IsPressed && !secondaryButtonPrevPressed)
            {
                lineManager.UndoLine(LineManager.DASH_LINE);
                secondaryButtonPrevPressed = true;
                return;
            }

            pointerMesh.position = userPointer.position;
            pointerMesh.rotation = Quaternion.LookRotation(userPointer.transform.forward, userPointer.transform.up);

            lineInfoLabel.text = "\npointer at " + pointerMesh.localPosition.ToString() + " \n";

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

        private void TryToGetPointer()
        {
            GameObject.FindObjectsByType<ControllerPivot>(FindObjectsSortMode.None)
                .Where(x => x.gameObject.name == "Head Angle")
                .Where(x => x.gameObject.transform.parent.transform.parent.transform.parent.name.Contains("Right"))
                .ToList()
                .LastOrDefault(x => userPointer = x.transform);

            if (userPointer != null)
            {
                UnityEngine.Debug.Log("User pointer found");
                pointerRenderer = pointerMesh.gameObject.GetComponentInChildren<Renderer>();
                pointerRenderer.material.color = new UnityEngine.Color(1f, 1f, 1f, 0.1f);
                pointerRenderer.enabled = false;
            }
            //else {
            //    UnityEngine.Debug.Log("Cant find user pointer!");
            //}
        }

        private void TryToEnableHaptics()
        {
            rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            rightHandDevice.TryGetHapticCapabilities(out hapticCapabilities);
            if (!hapticCapabilities.supportsImpulse)
            {
                UnityEngine.Debug.LogWarning("Right hand device does not support haptic impulses.");
            }
            else
            {
                UnityEngine.Debug.Log("Right hand device supports haptic impulses.");
                rightHandDevice.SendHapticImpulse(0, .5f, .1f); // Test haptic feedback
            }
        }
    }
}