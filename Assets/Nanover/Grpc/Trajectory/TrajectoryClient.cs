namespace Nanover.Grpc.Trajectory
{
    /// <summary>
    /// Wraps a <see cref="TrajectoryService.TrajectoryServiceClient" /> and
    /// provides access to a stream of frames from a trajectory provided by a
    /// server over a <see cref="GrpcConnection" />.
    /// </summary>
    public sealed class TrajectoryClient
    {
        /// <summary>
        /// Command the server to play the simulation if it is paused.
        /// </summary>
        public const string CommandPlay = "playback/play";
        
        /// <summary>
        /// Command the server to pause the simulation if it is playing.
        /// </summary>
        public const string CommandPause = "playback/pause";
        
        /// <summary>
        /// Command the server to advance by one simulation step.
        /// </summary>
        public const string CommandStep = "playback/step";

        /// <summary>
        /// Command the server to go back by one simulation step.
        /// </summary>
        public const string CommandStepBackward = "playback/step_back";

        /// <summary>
        /// Command the server to reset the simulation to its initial state.
        /// </summary>
        public const string CommandReset = "playback/reset";

        /// <summary>
        /// Fetch list of available simulations from server.
        /// </summary>
        public const string CommandGetSimulationsListing = "playback/list";

        /// <summary>
        /// Select from the simulations by index of the listing.
        /// </summary>
        public const string CommandSetSimulationIndex = "playback/load";

        /// <summary>
        /// Fetch list of available commands from server.
        /// </summary>
        public const string CommandGetCommandsListing = "commands/list";
    }
}