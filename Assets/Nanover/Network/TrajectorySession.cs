using Cysharp.Threading.Tasks;
using WebSocketTypes;
using Nanover.Frame;
using Nanover.Frame.Event;
using Nanover.Network.Frame;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

namespace Nanover.Network.Trajectory
{
    /// <summary>
    /// Adapts <see cref="TrajectoryClient" /> into an
    /// <see cref="ITrajectorySnapshot" /> where
    /// <see cref="ITrajectorySnapshot.CurrentFrame" /> is the latest received frame.
    /// </summary>
    public class TrajectorySession : ITrajectorySnapshot, IDisposable
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

        private List<float> messageReceiveTimes = new List<float>();
        public List<float> MessageReceiveTimes => messageReceiveTimes;

        private WebSocketMessageSource websocketClient;

        public TrajectorySession()
        {
            trajectorySnapshot.FrameChanged += (sender, args) => FrameChanged?.Invoke(sender, args);
        }

        public void OpenClient(WebSocketMessageSource client)
        {
            websocketClient = client;

            client.OnMessage += (Message message) =>
            {
                if (message.FrameUpdate is { } update)
                    ReceiveFrame(update);
            };

            void ReceiveFrame(Dictionary<string, object> update)
            {
                CurrentFrameIndex = CurrentFrameIndex + 1;

                var clear = false;
                var prevFrame = clear ? null : CurrentFrame;

                var (frame, changes) = FrameConverter.ConvertFrame(update, prevFrame);

                if (clear)
                    changes = FrameChanges.All;

                if (changes.HasAnythingChanged)
                    messageReceiveTimes.Add(Time.realtimeSinceStartup);

                trajectorySnapshot.SetCurrentFrame(frame, changes);
            }
        }

        /// <summary>
        /// Close the current trajectory client.
        /// </summary>
        public void CloseClient()
        {
            websocketClient = null;
            trajectorySnapshot.Clear();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            CloseClient();
        }
        
        /// <inheritdoc cref="CommandPlay"/>
        public void Play()
        {
            RunCommand(CommandPlay);
        }
        
        /// <inheritdoc cref="CommandPause"/>
        public void Pause()
        {
            RunCommand(CommandPause);
        }
        
        /// <inheritdoc cref="CommandReset"/>
        public void Reset()
        {
            RunCommand(CommandReset);
        }
        
        /// <inheritdoc cref="CommandStep"/>
        public void Step()
        {
            RunCommand(CommandStep);
        }

        /// <inheritdoc cref="CommandStepBackward"/>
        public void StepBackward()
        {
            RunCommand(CommandStepBackward);
        }

        // TODO: handle the non-existence of these commands
        /// <inheritdoc cref="CommandGetSimulationsListing"/>
        public async UniTask<List<string>> GetSimulationListing()
        {
            var result = await RunCommand(CommandGetSimulationsListing);
            var listing = result["simulations"] as IList<object>;

            return listing?.Select(o => o as string).ToList() ?? new List<string>();
        }

        /// <inheritdoc cref="TrajectoryClient.CommandSetSimulationIndex"/>
        public void SetSimulationIndex(int index)
        {
            RunCommand(CommandSetSimulationIndex, new CommandArguments { { "index", index } });
        }

        public UniTask<CommandReturn> RunCommand(string name, CommandArguments arguments = null)
        {
            return websocketClient?.RunCommand(name, arguments)
                ?? UniTask.FromCanceled<CommandReturn>();
        }

        public async UniTask<Dictionary<string, CommandDefinition>> UpdateCommands()
        {
            var result = await RunCommand(CommandGetCommandsListing);
            CommandDefinitions = ((Dictionary<string, object>)result["list"]).ToDictionary(pair => pair.Key, pair => new CommandDefinition { Name = pair.Key, Arguments = pair.Value as CommandArguments });
            return CommandDefinitions;
        }

        public class CommandDefinition
        {
            public string Name { get; set; }
            public CommandArguments Arguments { get; set; }
        }
    }
}