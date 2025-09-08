using Cysharp.Threading.Tasks;
using MessagePackTesting;
using Nanover.Frame;
using Nanover.Frame.Event;
using Nanover.Grpc.Frame;
using Nanover.Grpc.Stream;
using Nanover.Protocol.Command;
using Nanover.Protocol.Trajectory;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

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
        private BackgroundIncomingStreamReceiver<GetFrameResponse> frameReceiver;

        private List<float> messageReceiveTimes = new List<float>();
        public List<float> MessageReceiveTimes => frameReceiver?.MessageReceiveTimes ?? messageReceiveTimes;

        private WebSocket websocket;
        private WebSocketMessageSource websocketClient;

        public TrajectorySession()
        {
            trajectorySnapshot.FrameChanged += (sender, args) => FrameChanged?.Invoke(sender, args);
        }

        public void OpenClient(WebSocket websocket, WebSocketMessageSource client)
        {
            this.websocket = websocket;
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

            void ReceiveFrame2(FrameUpdate update)
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
        /// Connect to a trajectory service over the given connection and
        /// listen in the background for frame changes. Closes any existing
        /// client.
        /// </summary>
        public void OpenClient(GrpcConnection connection)
        {
            CloseClient();
            trajectorySnapshot.Clear();

            trajectoryClient = new TrajectoryClient(connection);
            frameStream = trajectoryClient.SubscribeLatestFrames(1f / 30f);
            frameReceiver = BackgroundIncomingStreamReceiver<GetFrameResponse>.Start(frameStream, ReceiveFrame, Merge);

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
        public void CloseClient()
        {
            websocket = null;
            websocketClient = null;

            trajectoryClient?.Close();
            trajectoryClient?.Dispose();
            trajectoryClient = null;

            frameStream?.Close();
            frameStream?.Dispose();
            frameStream = null;

            trajectorySnapshot.Clear();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            CloseClient();
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandPlay"/>
        public void Play()
        {
            RunCommand(TrajectoryClient.CommandPlay);
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandPause"/>
        public void Pause()
        {
            RunCommand(TrajectoryClient.CommandPause);
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandReset"/>
        public void Reset()
        {
            RunCommand(TrajectoryClient.CommandReset);
        }
        
        /// <inheritdoc cref="TrajectoryClient.CommandStep"/>
        public void Step()
        {
            RunCommand(TrajectoryClient.CommandStep);
        }

        /// <inheritdoc cref="TrajectoryClient.CommandStepBackward"/>
        public void StepBackward()
        {
            RunCommand(TrajectoryClient.CommandStepBackward);
        }

        // TODO: handle the non-existence of these commands
        /// <inheritdoc cref="TrajectoryClient.CommandGetSimulationsListing"/>
        public async UniTask<List<string>> GetSimulationListing()
        {
            var result = await RunCommand(TrajectoryClient.CommandGetSimulationsListing);
            var listing = result["simulations"] as IList<object>;

            return listing?.Select(o => o as string).ToList() ?? new List<string>();
        }

        /// <inheritdoc cref="TrajectoryClient.CommandSetSimulationIndex"/>
        public void SetSimulationIndex(int index)
        {
            RunCommand(TrajectoryClient.CommandSetSimulationIndex, new CommandArguments { { "index", index } });
        }

        public UniTask<CommandReturn> RunCommand(string name, CommandArguments arguments = null)
        {
            return websocketClient?.RunCommand(name, arguments) 
                ?? trajectoryClient?.RunCommandAsync(name, arguments)
                ?? UniTask.FromCanceled<CommandReturn>();
        }

        public async UniTask<Dictionary<string, CommandDefinition>> UpdateCommands()
        {
            var result = await RunCommand(TrajectoryClient.CommandGetCommandsListing);
            CommandDefinitions = ((Dictionary<string, object>)result["list"]).ToDictionary(pair => pair.Key, pair => new CommandDefinition { Name = pair.Key, Arguments = pair.Value as CommandArguments });
            return CommandDefinitions;
        }

        public class CommandDefinition
        {
            public string Name { get; set; }
            public CommandArguments Arguments { get; set; }

            public static CommandDefinition FromCommandMessage(CommandMessage message)
            {
                return new CommandDefinition()
                {
                    Name = message.Name,
                    Arguments = message.Arguments.ToDictionary(),
                };
            }
        }

        public bool Connected => trajectoryClient != null || websocket?.State == WebSocketState.Open;
    }
}