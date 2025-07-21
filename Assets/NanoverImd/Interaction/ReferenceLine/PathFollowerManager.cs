using Nanover.Frontend.XR;
using Nanover.Visualisation;
using NanoverImd;
using NanoverImd.PathFollower;
using UnityEngine;
using UnityEngine.XR;


public class PathFollowerManager : MonoBehaviour
{
    [SerializeField] private NanoverImdSimulation simulation;
    [SerializeField] private SynchronisedFrameSource frameSource;

    [SerializeField] private PathFollower pathFollower;

    [SerializeField] private GameObject linesManager;
    private LineRenderer pathToFollow;

    private Nanover.Frontend.Input.IButton primaryButton, secondaryButton;

    private int lastAtomIndex = -1;


    void Start()
    {
        primaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.primaryButton);
        secondaryButton = InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.secondaryButton);

        secondaryButton.Pressed += () =>
        {
            Debug.Log("PathFollowerManager: Secondary button pressed, disabling path follower.");
            pathFollower.enabled = false;
        };
    }

    void Update()
    {
        if (simulation == null || frameSource == null) return;
        ProcessFrameData(out lastAtomIndex);

        if (lastAtomIndex != -1 && !pathFollower.enabled)
        {
            Debug.Log($"PathFollowerManager: Interaction detected, attempting to follow path with atom index {lastAtomIndex}");
            pathToFollow = GetLastReferencePath();
            if (pathToFollow != null)
            {
                UnityEngine.Debug.Log($"PathFollowerManager: Following path with last atom index {lastAtomIndex}");
                pathFollower.AtomId = lastAtomIndex;
                pathFollower.testLine = pathToFollow;
                pathFollower.enabled = true;
            }
        }
    }
    
    LineRenderer GetLastReferencePath()
    {
        // get all line renderers inside the linesManager game object
        LineRenderer[] lines = linesManager.GetComponentsInChildren<LineRenderer>();

        // find the last line renderer that has "reference" in its name
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            if (lines[i].name.Contains("dash"))
            {
                return lines[i];
            }
        }
        return null;
    }

    void ProcessFrameData(out int atom)
    {
        atom = -1;

        if (frameSource.CurrentFrame == null)
        {
            return;
        }

        var data = frameSource.CurrentFrame.Data;
        
        if (data.TryGetValue("forces.user.index", out var capturedSelectedAtoms))
        {
            if (capturedSelectedAtoms is uint[] selectedAtoms && selectedAtoms.Length == 1)
            {
                atom = (int)selectedAtoms[0];
                return;
            }
        }
    }
}
