using System;
using System.Collections.Generic;
using Nanover.Frontend.XR;
using Nanover.Visualisation;
using NanoverImd;
using NanoverImd.PathFollower;
using TMPro;
using UnityEngine;
using UnityEngine.XR;


public class InteractionTrailsManager : MonoBehaviour
{
    [SerializeField] private LineManager lineManager;
    [SerializeField] private NanoverImdSimulation simulation;
    [SerializeField] private SynchronisedFrameSource frameSource;
    [SerializeField] private ParticleRelativeSpace pathSpace;
    [SerializeField] private SimulationInformationDisplay simulationInformationDisplay;

    private long currentLineTimestamp = -1;
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

    // Store all created line timestamps
    private List<long> createdLineTimestamps = new();

    public void OnDisconnect()
    {
        createdLineTimestamps.Clear();
        currentLineTimestamp = -1;
        lastAtomIndex = null;
        lastPosition = null;
    }

    private void OnEnable()
    {
        // Haptics setup
        rightHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        rightHandDevice.TryGetHapticCapabilities(out hapticCapabilities);
        hasHaptics = hapticCapabilities.supportsImpulse;

        yButton = InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.secondaryButton);
        yButton.Pressed += () =>
        {
            lineManager.RemoveAllLines(LineManager.SOLID_LINE);
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

        int? atomIndex = GetSelectedAtomIndex();

        lastAtomIndex = atomIndex;

        Vector3? newPosition = GetInteractionPositionFromAtoms(simulation);

        if (newPosition != null) lastPosition = newPosition;

        float? currentWork = GetCurrentWork(data);
        if (currentWork != null)
        {
            lastWork = currentWork;
        }

        if (newPosition.HasValue && newPosition.Value.magnitude > 0)
        {
            RegisterCurrentWork(lastWork, ref workSnapshots);

            float? frameIndex = GetFrameTimestamp(data);
            if (frameIndex == null) return;

            // Start a new line if needed (e.g., on new interaction)
            if (currentLineTimestamp == -1 || frameIndex - lastFrameIndex > 0.3f)
            {
                currentLineTimestamp = lineManager.CreateNewLine(LineManager.SOLID_LINE);

                // Save the new line timestamp
                createdLineTimestamps.Add(currentLineTimestamp);
            }

            lastFrameIndex = frameIndex;

            lineManager.AddPointToLine(currentLineTimestamp, pathSpace.PositionFromSimulationToPath(newPosition.Value));

            UpdateInfo();
        }
    }

    private void UpdateInfo()
    {
        // Get the line renderer using timestamp
        var lines = GetComponentsInChildren<LineRenderer>();
        LineRenderer line = null;
        
        foreach (var lr in lines)
        {
            if (lr.name.Contains(currentLineTimestamp.ToString()))
            {
                line = lr;
                break;
            }
        }
        
        if (line == null) return;
        
        float length = 0; // We'll need to update this method or calculate length differently
        int numPoints = line.positionCount;
        float lineSmoothnessA = LineManager.CalculateAngularSmoothness(line) / Mathf.PI;
        float lineSmoothnessB = LineManager.CalculateSmoothness(line);

        // HAPTIC
        float f = Mathf.Abs(deltaWork / 50);
        rightHandDevice.SendHapticImpulse(0, f, 0.01f);
    }

    int? GetSelectedAtomIndex()
    {
        if (frameSource?.CurrentFrame?.Data is { } data)
            if (data.TryGetValue("forces.user.index", out var interactedAtomsObj)
            && interactedAtomsObj is object[] interactedAtoms
            && interactedAtoms.Length == 1)
            {
                return Convert.ToInt32(interactedAtoms[0]);
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
            if (capturedSelectedAtoms is object[] selectedAtoms) { 
                return computeParticleCentroid(selectedAtoms);
            }
        }
        return Vector3.zero;
    }

    private Vector3 computeParticleCentroid(object[] particleIds)
    {
        var centroid = Vector3.zero;

        for (int i = 0; i < particleIds.Length; ++i)
            centroid += simulation.FrameSynchronizer.CurrentFrame.ParticlePositions[Convert.ToInt32(particleIds[i])];  // todo: parametrize this or relocate this as inline function

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
