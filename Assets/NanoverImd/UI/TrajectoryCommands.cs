using NanoverImd;
using UnityEngine;

namespace NanoverImd
{
    /// <summary>
    /// Component that exposes the trajectory playback commands to Unity UI
    /// components.
    /// </summary>
    public sealed class TrajectoryCommands : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;
#pragma warning restore 0649

        public void SendPlayCommand() => simulation.PlayTrajectory();
        public void SendPauseCommand() => simulation.PauseTrajectory();
        public void SendStepCommand() => simulation.StepForwardTrajectory();
        public void SendResetCommand() => simulation.ResetTrajectory();
    }
}