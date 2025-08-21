using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nanover.Core.Math;
using Nanover.Grpc.Stream;
using Nanover.Grpc.Trajectory;
using UnityEngine;
using NativeWebSocket;
using MessagePackTesting;

namespace Nanover.Grpc.Multiplayer
{
    /// <summary>
    /// Manages the state of a single user engaging in multiplayer. Tracks
    /// local and remote avatars and maintains a copy of the latest values in
    /// the shared key/value store.
    /// </summary>
    public sealed class MultiplayerSession : IDisposable
    {
        public const string SimulationPoseKey = "scene";

        public string AccessToken { get; set; }

        public MultiplayerAvatars Avatars { get; }
        public PlayAreaCollection PlayAreas { get; }
        public PlayOriginCollection PlayOrigins { get; }

        public MultiplayerSession()
        {
            Avatars = new MultiplayerAvatars(this);
            PlayAreas = new PlayAreaCollection(this);
            PlayOrigins = new PlayOriginCollection(this);

            SimulationPose =
                new MultiplayerResource<Transformation>(this, SimulationPoseKey, PoseFromObject,
                                                        PoseToObject);
        }

        /// <summary>
        /// The transformation of the simulation box.
        /// </summary>
        public readonly MultiplayerResource<Transformation> SimulationPose;

        /// <summary>
        /// Dictionary of the currently known shared state.
        /// </summary>
        public Dictionary<string, object> SharedStateDictionary { get; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Is there an open client on this session?
        /// </summary>
        public bool IsOpen => (client != null || websocket != null) && !closing;

        private bool closing = false;

        /// <summary>
        /// How many milliseconds to put between sending our requested value
        /// changes.
        /// </summary>
        public int ValuePublishInterval { get; set; } = 1000 / 30;

        private MultiplayerClient client;

        private IncomingStream<Protocol.State.StateUpdate> IncomingValueUpdates { get; set; }
        private BackgroundIncomingStreamReceiver<Protocol.State.StateUpdate> updateReceiver;
        private List<float> messageReceiveTimes = new List<float>();

        public List<float> MessageReceiveTimes => updateReceiver?.MessageReceiveTimes ?? messageReceiveTimes;

        private Dictionary<string, object> pendingValues
            = new Dictionary<string, object>();

        private List<string> pendingRemovals
            = new List<string>();

        private UniTask? valueFlushingTask;

        public event Action<string, object> SharedStateDictionaryKeyUpdated;

        public event Action<string> SharedStateDictionaryKeyRemoved;

        public event Action MultiplayerJoined;

        /// <summary>
        /// The index of the next update that we will send to the server. A key
        /// `update.index.{player_id}` will be inserted with this value. By getting this value
        /// when you've scheduled something to be done to the dictionary, you can then determine
        /// when a returned update has incorporated your change.
        /// </summary>
        public int NextUpdateIndex => nextUpdateIndex;

        /// <summary>
        /// The index of the latest changes we sent to the server which have been received by us.
        /// </summary>
        public int LastReceivedIndex => lastReceivedIndex;

        public float TimeSinceIndex => AwaitingIndex ? Time.realtimeSinceStartup - lastReceivedIndexTime : 0;
        public bool AwaitingIndex => IsOpen && lastReceivedIndexTime != -1 && lastSentIndex > lastReceivedIndex;

        private int nextUpdateIndex = 0;

        private int lastSentIndex = -1;
        private int lastReceivedIndex = -1;
        private float lastReceivedIndexTime = -1;

        public float LastIndexRTT { get; private set; } = -1;

        private string UpdateIndexKey => $"update.index.{AccessToken}";

        private Dictionary<int, float> updateSendTimes = new Dictionary<int, float>();
        private WebSocket websocket;
        private Func<Message, UniTask> SendMessage;

        public void OpenClient(WebSocket websocket, WebSocketMessageSource source, Func<Message, UniTask> SendMessage)
        {
            this.SendMessage = SendMessage;

            this.websocket = websocket;
            source.OnMessage += (message) =>
            {
                if (message.StateUpdate is { } update)
                    ReceiveState(update);
            };

            void ReceiveState(StateUpdate update)
            {
                messageReceiveTimes.Add(Time.realtimeSinceStartup);

                if (update.Updates.ContainsKey(UpdateIndexKey))
                {
                    lastReceivedIndex = Convert.ToInt32(update.Updates[UpdateIndexKey]);
                    lastReceivedIndexTime = Time.realtimeSinceStartup;

                    foreach (var index in new HashSet<int>(updateSendTimes.Keys))
                    {
                        if (index == lastReceivedIndex)
                            LastIndexRTT = lastReceivedIndexTime - updateSendTimes[index];

                        if (index <= lastReceivedIndex)
                            updateSendTimes.Remove(index);
                    }
                }

                foreach (var key in update.Removals)
                {
                    SharedStateDictionary.Remove(key);
                    SharedStateDictionaryKeyRemoved?.Invoke(key);
                }

                foreach (var (key, value) in update.Updates)
                {
                    SharedStateDictionary[key] = value;
                    SharedStateDictionaryKeyUpdated?.Invoke(key, value);
                }
            }

            AccessToken = Guid.NewGuid().ToString();

            RunFlushingTask();

            MultiplayerJoined?.Invoke();
        }

        /// <summary>
        /// Connect to a Multiplayer service over the given connection. 
        /// Closes any existing client.
        /// </summary>
        public async UniTask OpenClient(GrpcConnection connection)
        {
            await CloseClient();
            closing = false;

            client = new MultiplayerClient(connection);
            AccessToken = Guid.NewGuid().ToString();

            RunFlushingTask();

            IncomingValueUpdates = client.SubscribeStateUpdates();
            updateReceiver = BackgroundIncomingStreamReceiver<Protocol.State.StateUpdate>.Start(
                IncomingValueUpdates,
                OnResourceValuesUpdateReceived,
                MergeResourceUpdates
            );

            void MergeResourceUpdates(Protocol.State.StateUpdate dest, Protocol.State.StateUpdate src)
            {
                foreach (var (key, value) in src.ChangedKeys.Fields)
                    dest.ChangedKeys.Fields[key] = value;
            }

            MultiplayerJoined?.Invoke();
        }

        private void RunFlushingTask()
        {
            valueFlushingTask = valueFlushingTask ?? FlushValuesInterval(ValuePublishInterval);

            async UniTask FlushValuesInterval(int interval)
            {
                try
                {
                    while (true)
                    {
                        FlushValuesAsync().Forget();

                        await UniTask.Delay(interval);
                    }
                }
                finally
                {
                    valueFlushingTask = null;
                }
            }
        }


        /// <summary>
        /// Close the current Multiplayer client and dispose all streams.
        /// </summary>
        public async UniTask CloseClient()
        {
            ClearSharedState();

            lastReceivedIndex = -1;
            lastSentIndex = -1;
            lastReceivedIndexTime = -1;

            LastIndexRTT = -1;

            if (!IsOpen)
                return;

            closing = true;

            IncomingValueUpdates?.Close();
            IncomingValueUpdates = null;

            // Remove our personal avatar/playarea/origin
            SimulationPose.ReleaseLock();
            Avatars.CloseClient();
            PlayAreas.RemoveValue(AccessToken ?? "");
            PlayOrigins.RemoveValue(AccessToken ?? "");
            RemoveSharedStateKey(UpdateIndexKey);

            await UniTask.WhenAny(FlushValuesAsync(), UniTask.Delay(1000));

            client?.Close();
            client?.Dispose();
            client = null;

            updateSendTimes.Clear();

            AccessToken = null;
        }

        /// <summary>
        /// Set the given key in the shared state dictionary, which will be
        /// sent to the server according in the future according to the publish 
        /// interval.
        /// </summary>
        public void SetSharedState(string key, object value)
        {
            pendingValues[key] = value;
            pendingRemovals.Remove(key);
        }
        
        /// <summary>
        /// Remove the given key from the shared state dictionary, which will be
        /// sent to the server according in the future according to the publish 
        /// interval.
        /// </summary>
        public void RemoveSharedStateKey(string key)
        {
            pendingValues.Remove(key);
            pendingRemovals.Add(key);
        }


        /// <summary>
        /// Get a key in the shared state dictionary.
        /// </summary>
        public object GetSharedState(string key)
        {
            return SharedStateDictionary.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Attempt to gain exclusive write access to the shared value of the given key.
        /// </summary>
        public async UniTask<bool> LockResource(string id)
        {
            if (websocket != null)
                return true;

            return await client.UpdateLocks(AccessToken, new Dictionary<string, float>
                                            {
                                                [id] = 1f
                                            },
                                            new string[0]);
        }

        /// <summary>
        /// Release the lock on the given object of a given key.
        /// </summary>
        public async UniTask<bool> ReleaseResource(string id)
        {
            if (websocket != null)
                return true;

            return await client.UpdateLocks(AccessToken, new Dictionary<string, float>(), new string[]
            {
                id
            });
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            CloseClient().Forget();
        }

        private void ClearSharedState()
        {
            pendingValues.Clear();
            pendingRemovals.Clear();

            var keys = SharedStateDictionary.Keys.ToList();
            SharedStateDictionary.Clear();

            foreach (var key in keys)
            {
                try
                {
                    SharedStateDictionaryKeyRemoved?.Invoke(key);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void OnResourceValuesUpdateReceived(Protocol.State.StateUpdate update)
        {
            if (!IsOpen)
                return;

            if (update.ChangedKeys.Fields.ContainsKey(UpdateIndexKey))
            {
                lastReceivedIndex = (int) update.ChangedKeys
                                                .Fields[UpdateIndexKey]
                                                .NumberValue;
                lastReceivedIndexTime = Time.realtimeSinceStartup;

                foreach (var index in new HashSet<int>(updateSendTimes.Keys))
                {
                    if (index == lastReceivedIndex)
                        LastIndexRTT = lastReceivedIndexTime - updateSendTimes[index];
                    
                    if (index <= lastReceivedIndex)
                        updateSendTimes.Remove(index);
                }
            }

            foreach (var (key, value1) in update.ChangedKeys.Fields)
            {
                var value = value1.ToObject();
                if (value == null)
                {
                    SharedStateDictionary.Remove(key);
                    SharedStateDictionaryKeyRemoved?.Invoke(key);
                }
                else
                {
                    SharedStateDictionary[key] = value;
                    SharedStateDictionaryKeyUpdated?.Invoke(key, value);
                }
            }
        }
        
        public void Heartbeat()
        {
            pendingValues[UpdateIndexKey] = nextUpdateIndex;
            lastSentIndex = nextUpdateIndex;
            updateSendTimes[nextUpdateIndex] = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Attempts to send all pending updates to the server and returns
        /// false if there were pending changes that failed to send, or true
        /// otherwise.
        /// </summary>
        private async UniTask<bool> FlushValuesAsync()
        {
            if (!pendingValues.Any() && !pendingRemovals.Any())
                return true;

            if (!IsOpen)
                return false;

            if (!pendingRemovals.Contains(UpdateIndexKey))
            {
                Heartbeat();
            }

            var update = UniTask.FromResult(true);

            if (client != null)
            {
                var converted = new Dictionary<string, object>();

                foreach (var (key, value) in pendingValues)
                    converted[key] = value.ToProtobufValue();

                update = client.UpdateState(AccessToken, converted, pendingRemovals);
            }
            else if (websocket != null)
            {
                var change = new StateUpdate();
                foreach (var (key, value) in pendingValues)
                    change.Updates[key] = value;
                change.Removals.UnionWith(pendingRemovals);

                var message = new Message
                {
                    StateUpdate = change,
                };

                update = SendMessage(message).ContinueWith(() => true);
            }

            pendingValues.Clear();
            pendingRemovals.Clear();

            nextUpdateIndex++;

            return await update;
        }

        private static object PoseToObject(Transformation pose)
        {
            var data = new object[]
            {
                pose.Position.x, pose.Position.y, pose.Position.z, pose.Rotation.x, pose.Rotation.y,
                pose.Rotation.z, pose.Rotation.w, pose.Scale.x, pose.Scale.y, pose.Scale.z,
            };

            return data;
        }

        private static Transformation PoseFromObject(object @object)
        {
            if (@object is IReadOnlyList<object> list)
            {
                var values = list.Select(value => Convert.ToSingle(value)).ToList();
                var position = list.GetVector3(0);
                var rotation = list.GetQuaternion(3);
                var scale = list.GetVector3(7);

                return new Transformation(position, rotation, scale);
            }
            else if (@object is null)
            {
                return Transformation.Identity;
            }

            throw new ArgumentOutOfRangeException();
        }

        public MultiplayerResource<object> GetSharedResource(string key)
        {
            return new MultiplayerResource<object>(this, key);
        }
    }
}