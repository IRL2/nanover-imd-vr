using System.Collections.Generic;
using Nanover.Frontend.XR;
using Nanover.Visualisation;
using NanoverImd;
using NanoverImd.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;


public class InteractionTrailsManager : MonoBehaviour
{
    [SerializeField] private LineManager lineManager;
    [SerializeField] private NanoverImdSimulation simulation;
    [SerializeField] private SynchronisedFrameSource frameSource;
    //[SerializeField] private Transform simulationParent;
    [SerializeField] private TextMeshPro infoLabel;

    private int currentLineIndex = -1;
    private int? lastAtomIndex;
    private float? lastFrameIndex = 0;
    private Vector3? lastPosition = Vector3.zero;
    private float? lastWork = 0.0f;
    private float deltaWork = 0.0f;

    private List<float> workSnapshots = new();
    private bool hasHaptics = false;
    private UnityEngine.XR.InputDevice rightHandDevice;
    private UnityEngine.XR.HapticCapabilities hapticCapabilities;

    [SerializeField] private Nanover.Frontend.Input.IButton yButton;

    private float currentColorHue = 0.5f;

    // Add this field to store all created line indices
    private List<int> createdLineIndices = new();

    private void OnEnable()
    {
        // Haptics setup
        rightHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        rightHandDevice.TryGetHapticCapabilities(out hapticCapabilities);
        hasHaptics = hapticCapabilities.supportsImpulse;

        yButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.secondaryButton);
        yButton.Pressed += () =>
        {
            lineManager.UndoLine(LineManager.SOLID_LINE);
            UpdateInfo();
        };
    }

    void Update()
    {
        // this should go to the HapticController class
        if (!hasHaptics)
        {
            rightHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            rightHandDevice.TryGetHapticCapabilities(out hapticCapabilities);
            hasHaptics = hapticCapabilities.supportsImpulse;
            if (hasHaptics)
                rightHandDevice.SendHapticImpulse(0, 0.5f, 0.1f);
            else
                return;
        }

        if (simulation == null || frameSource == null) return;
        ProcessFrameData();

    }

    private void ProcessFrameData()
    {
        if (frameSource.CurrentFrame == null) return;
        var data = frameSource.CurrentFrame.Data;


        int? atomIndex = GetSelectedAtomIndex(data);
        //if (atomIndex == null) return;
        lastAtomIndex = atomIndex;

        //Vector3? newPosition = GetPositionFromAtom(atomIndex.Value);

        Vector3? newPosition = GetInteractionPositionFromAtoms(simulation);


        if (newPosition != null) lastPosition = newPosition;


        float? currentWork = GetCurrentWork(data);
        if (currentWork != null) lastWork = currentWork;

        if (newPosition.HasValue && newPosition.Value.magnitude > 0)
        {
            RegisterCurrentWork(lastWork, ref workSnapshots);

            float? frameIndex = GetFrameTimestamp(data);
            if (frameIndex == null) return;

            // Start a new line if needed (e.g., on new interaction)
            if (currentLineIndex == -1 || frameIndex - lastFrameIndex > 0.3f)
            {
                //Debug.Log($" started a new line at frame ${lastFrameIndex}");
                //lineManager.SimplifyLine(currentLineIndex, 0.001f);

                currentLineIndex = lineManager.CreateNewLine(LineManager.SOLID_LINE);

                // Save the new line index
                createdLineIndices.Add(currentLineIndex);

                currentColorHue = (currentColorHue + 0.1f) % 1.0f;
                lineManager.SetLineColor(currentLineIndex,
                                         Color.HSVToRGB(currentColorHue, 0.85f, 0.85f));
            }

            lastFrameIndex = frameIndex;

            lineManager.AddPointToLine(currentLineIndex, newPosition.Value);
            UpdateInfo();
        }
    }


    private void UpdateInfo()
    {
        if (infoLabel == null) return;
        var line = lineManager.GetLineRenderer(currentLineIndex);
        if (line == null) return;
        float length = lineManager.GetLineLength(currentLineIndex);
        int numPoints = line.positionCount;
        float lineSmoothnessA = LineManager.CalculateAngularSmoothness(line) / Mathf.PI;
        float lineSmoothnessB = LineManager.CalculateSmoothness(line);
        float f = Mathf.Abs(deltaWork / 50);
        rightHandDevice.SendHapticImpulse(0, f, 0.01f);

        string info = $"<u>trajectory trail line</u>\n" +
                        $"lenght is {length:F2} nm\n" +
                        $"having {numPoints} points\n" +
                        $"from {line.GetPosition(0):F2}\n" +
                        $"to {line.GetPosition(line.positionCount - 1):F2}\n" +
                        $"angular triplets {(lineSmoothnessA * 100):F1}%\n" +
                        $"path jagger is {lineSmoothnessB:F2}\n\n" +
                        $"<u>system information</u>\nrelative work:{deltaWork} \n" +
                        $"last interaction atom is #{lastAtomIndex} \n";
        infoLabel.text = info;
    }

    private int? GetSelectedAtomIndex(IDictionary<string, object> data)
    {
        if (data.TryGetValue("forces.user.index", out var capturedSelectedAtoms))
        {
            if (capturedSelectedAtoms is uint[] selectedAtoms && selectedAtoms.Length == 1)
            {
                return (int)selectedAtoms[0];
            }
        }
        return null;
    }

    private Vector3 GetInteractionPositionFromAtoms(NanoverImdSimulation sim)
    {
        var interactions = sim.Interactions;
        var frame = sim.FrameSynchronizer.CurrentFrame;

        IDictionary<string, object> data = frame.Data;

        if (data.TryGetValue("forces.user.index", out var capturedSelectedAtoms))
        {
            if (capturedSelectedAtoms is uint[] selectedAtoms) { 
                return computeParticleCentroid(selectedAtoms);
            }
        }
        return Vector3.zero;
    }

    private Vector3 computeParticleCentroid(uint[] particleIds)
    {
        var centroid = Vector3.zero;

        for (int i = 0; i < particleIds.Length; ++i)
            centroid += simulation.FrameSynchronizer.CurrentFrame.ParticlePositions[particleIds[i]];  // todo: parametrize this or relocate this as inline function

        return centroid / particleIds.Length;
    }

    private float? GetFrameTimestamp(IDictionary<string, object> data)
    {
        if (data.TryGetValue("server.timestamp", out var frameIndex))
        {
            return (float)(double)frameIndex;
        }
        return null;
    }

    private Vector3? GetPositionFromAtom(int atomIndex)
    {
        if (frameSource.CurrentFrame.Data.TryGetValue("particle.positions", out var capturedParticlePositons))
        {
            if (capturedParticlePositons is Vector3[] particlePositons && particlePositons.Length > 0)
            {
                return particlePositons[atomIndex];
            }
        }
        return null;
    }

    private float? GetCurrentWork(IDictionary<string, object> data)
    {
        if (data.TryGetValue("forces.user.work_done", out var capturedWork))
        {
            return (float)(double)capturedWork;
        }
        return null;
    }

    private void RegisterCurrentWork(float? work, ref List<float> work_array)
    {
        if (work == null) return;
        if (work_array.Count > 1)
            deltaWork = work_array[^1] - (float)work;
        work_array.Add((float)(double)work);
    }
}
