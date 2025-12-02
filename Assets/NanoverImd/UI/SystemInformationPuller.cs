using Nanover.Visualisation;
using NanoverImd.PathFollower;
using UnityEngine;
using static SimulationInformationDisplay;

public class SystemInformationPuller : MonoBehaviour
{
    [SerializeField] private SynchronisedFrameSource frame;

    [SerializeField] private SimulationInformationDisplay display;

    [SerializeField] private PathFollower follower;

    [SerializeField] private LineManager lineManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (frame == null)
        {
            UnityEngine.Debug.LogError("SystemInformationPuller requires a SynchronisedFrameSource to function. Please assign one in the inspector.");
            return;
        }
    }

    void LateUpdate()
    {
        // maybe this is called not too often
        if (Time.frameCount % 4 == 0)
        {
            if (frame.CurrentFrame == null) return;
            var data = frame.CurrentFrame.Data;

            if (data.TryGetValue("forces.user.work_done", out var work))
            {
                display.UpdateData(DataKeys.accumulatedWork, ((float)(double)work).ToString("F2") + "kJ·mol^(-1)");
            }

            if (data.TryGetValue("system.simulation.time", out var simTime))
            {
                display.UpdateData(DataKeys.simulationTime, ((float)(double)simTime).ToString("F2") + "ps");
            }

            lineManager.GetAmountOfLines(out int numRefLines, out int numTrailLines);
            display.UpdateData(DataKeys.numRefLines, numRefLines.ToString());
            display.UpdateData(DataKeys.numTrailLines, numTrailLines.ToString());

            display.UpdateData(DataKeys.advance, $"{follower.LengthFollowed.ToString("F2")}nm");
            display.UpdateData(DataKeys.speed, $"{follower.Speed}nm/ps");
            display.UpdateData(DataKeys.forceScale, $"{follower.Scale}x");
        }

    }
}
