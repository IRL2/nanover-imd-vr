using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nanover.Core.Async;
using Nanover.Frame;
using Nanover.Frame.Event;
using Nanover.Grpc.Frame;
using Nanover.Grpc.Stream;
using Nanover.Protocol.Command;
using Nanover.Protocol.Trajectory;

namespace Nanover.Grpc.Trajectory
{
    /// <summary>
    /// Adapts <see cref="TrajectoryClient" /> into an
    /// <see cref="ITrajectorySnapshot" /> where
    /// <see cref="ITrajectorySnapshot.CurrentFrame" /> is the latest received frame.
    /// </summary>
    public class TrajectorySession : ITrajectorySnapshot, IDisposable
    {
        /// <inheritdoc cref="ITrajectorySnapshot.CurrentFrame" />
        public Nanover.Frame.Frame CurrentFrame => trajectorySnapshot.CurrentFrame;
        
        public int CurrentFrameIndex { get; private set; }

        public Dictionary<string, CommandDefinition> CommandDefinitions { get; private set; } = new Dictionary<string, CommandDefinition>();

        /// <inheritdoc cref="ITrajectorySnapshot.FrameChanged" />
        public event FrameChanged FrameChanged;

        /// <summary>
        /// Underlying <see cref="TrajectorySnapshot" /> for tracking
        /// <see cref="CurrentFrame" />.
        /// </summary>
        private readonly TrajectorySnapshot trajectorySnapshot = new TrajectorySnapshot();

        /// <summary>
        /// Underlying TrajectoryClient for receiving new frames.
        /// </summary>
        private TrajectoryClient trajectoryClient;

        private IncomingStream<GetFrameResponse> frameStream;

        public TrajectorySession()
        {
            trajectorySnapshot.FrameChanged += (sender, args) => FrameChanged?.Invoke(sender, args);
        }

        /// <summary>
        /// Connect to a trajectory service over the given connection and
        /// listen in the background for frame changes. Closes any existing
        /// client.
        /// </summary>
        public async Task OpenClient(GrpcConnection connection)
        {
            await CloseClient();
            trajectorySnapshot.Clear();

            trajectoryClient = new TrajectoryClient(connection);
            frameStream = trajectoryClient.SubscribeLatestFrames(1f / 30f);
            BackgroundIncomingStreamReceiver<GetFrameResponse>.Start(frameStream, ReceiveFrame, Merge);

            // Integrating frames from the buffer with the current frame
            void ReceiveFrame(GetFrameResponse response)
            {
                CurrentFrameIndex = (int) response.FrameIndex;

                var nextFrame = response.Frame;
                var clear = ContainsClear(response);
                var prevFrame = clear ? null : CurrentFrame;

                var (frame, changes) = FrameConverter.ConvertFrame(nextFrame, prevFrame);

                if (clear)
                    changes = FrameChanges.All;

                trajectorySnapshot.SetCurrentFrame(frame, changes);
            }

            // Aggregating frames while they wait in the buffer
            void Merge(GetFrameResponse dest, GetFrameResponse toMerge)
            {
                if (ContainsClear(toMerge))
                    dest.Frame = new FrameData();

                if (!ContainsClear(dest))
                    dest.FrameIndex = toMerge.FrameIndex;

                foreach (var (key, array) in toMerge.Frame.Arrays)
                    dest.Frame.Arrays[key] = array;
                foreach (var (key, value) in toMerge.Frame.Values)
                    dest.Frame.Values[key] = value;
            }

            // Does the frame indicate that previous frame contents should be
            // cleared?
            bool ContainsClear(GetFrameResponse response)
            {
                return response.FrameIndex == 0;
            }
        }

        /// <summary>
        /// Close the current trajectory client.
        /// </summary>
        public async Task CloseClient()
        {
            trajectoryClient?.CloseAndCancelAllSubscriptions();
            trajectoryClient?.Dispose();
            trajectoryClient = null;

            frameStream?.CloseAsync();
            frameStream?.Dispose();
            frameStream = null;

            trajectorySnapshot.Clear();

            await Task.CompletedTask;
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            CloseClient().AwaitInBackgroundIgnoreCancellation();
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandPlay"/>
        public void Play()
        {
            trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandPlay);
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandPause"/>
        public void Pause()
        {
            trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandPause);
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandReset"/>
        public void Reset()
        {
            trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandReset);
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandStep"/>
        public void Step()
        {
            trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandStep);
        }

        /// <inheritdoc cref="TrajectoryClient.CommandStepBackward"/>
        public void StepBackward()
        {
            trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandStepBackward);
        }

        // TODO: handle the non-existence of these commands
        /// <inheritdoc cref="TrajectoryClient.CommandGetSimulationsListing"/>
        public async Task<List<string>> GetSimulationListing()
        {
            var result = await trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandGetSimulationsListing);
            var listing = result["simulations"] as List<object>;
            return listing?.ConvertAll(o => o as string) ?? new List<string>();
        }

        /// <inheritdoc cref="TrajectoryClient.CommandSetSimulationIndex"/>
        public void SetSimulationIndex(int index)
        {
            trajectoryClient?.RunCommandAsync(TrajectoryClient.CommandSetSimulationIndex, new Dictionary<string, object> { { "index", index } });
        }

        public void RunCommand(string name, Dictionary<string, object> arguments = null)
        {
            trajectoryClient?.RunCommandAsync(name, arguments);
        }

        public Task<Dictionary<string, object>> RunCommandAsync(string name, Dictionary<string, object> arguments = null) => trajectoryClient?.RunCommandAsync(name, arguments);

        public async Task<Dictionary<string, CommandDefinition>> UpdateCommands()
        {
            var commands = await trajectoryClient?.GetCommandsAsync();
            CommandDefinitions = commands.ToDictionary(command => command.Name, CommandDefinition.FromCommandMessage);
            return CommandDefinitions;
        }

        public class CommandDefinition
        {
            public string Name { get; set; }
            public Dictionary<string, object> Arguments { get; set; }

            public static CommandDefinition FromCommandMessage(CommandMessage message)
            {
                return new CommandDefinition()
                {
                    Name = message.Name,
                    Arguments = message.Arguments.ToDictionary(),
                };
            }
        }

        public TrajectoryClient Client => trajectoryClient;
    }
}