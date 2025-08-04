using Nanover.Visualisation;
using UnityEngine;

public class SystemInformationPuller : MonoBehaviour
{
    [SerializeField] private SynchronisedFrameSource frame;

    [SerializeField] private SimulationInformationDisplay display;

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
        if (Time.frameCount % 2 == 0)
        {
            if (frame.CurrentFrame == null) return;
            var data = frame.CurrentFrame.Data;

            if (data.TryGetValue("forces.user.work_done", out var work))
            {
                display.UpdateData("accumulatedWork", ((float)(double)work).ToString("F2") + " mA");
            }

            if (data.TryGetValue("system.simulation.time", out var simTime))
            {
                display.UpdateData("simulationTime", ((float)(double)simTime).ToString("F2") + " ps");
            }
        }
    }


}
