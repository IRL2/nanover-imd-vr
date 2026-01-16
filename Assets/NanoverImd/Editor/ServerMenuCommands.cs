using UnityEditor;
using UnityEngine;

namespace NanoverImd.Editor
{
    /// <summary>
    /// Menu commands for debugging server commands.
    /// </summary>
    public static class ServerMenuCommands
    {
        /// <summary>
        /// Play the current server.
        /// </summary>
        [MenuItem("Nanover/Commands/Play")]
        public static void PlayServer()
        {
            Object.FindFirstObjectByType<NanoverImdApplication>().Simulation.PlayTrajectory();
        }

        /// <summary>
        /// Pause the current server.
        /// </summary>
        [MenuItem("Nanover/Commands/Pause")]
        public static void PauseServer()
        {
            Object.FindFirstObjectByType<NanoverImdApplication>().Simulation.PauseTrajectory();
        }

        /// <summary>
        /// Reset the current server.
        /// </summary>
        [MenuItem("Nanover/Commands/Reset")]
        public static void ResetServer()
        {
            Object.FindFirstObjectByType<NanoverImdApplication>().Simulation.ResetTrajectory();
        }

        /// <summary>
        /// Step the current server.
        /// </summary>
        [MenuItem("Nanover/Commands/Step")]
        public static void StepServer()
        {
            Object.FindFirstObjectByType<NanoverImdApplication>().Simulation.StepForwardTrajectory();
        }
    }
}