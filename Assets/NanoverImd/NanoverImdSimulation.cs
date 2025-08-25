using Cysharp.Threading.Tasks;
using Essd;
using MessagePackTesting;
using Nanover.Core.Math;
using Nanover.Frontend.Manipulation;
using Nanover.Grpc;
using Nanover.Grpc.Multiplayer;
using Nanover.Grpc.Trajectory;
using Nanover.Visualisation;
using NanoverImd.Interaction;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Nerdbank.MessagePack;

using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NanoverImd
{
    public class NanoverImdSimulation : MonoBehaviour, WebSocketMessageSource
    {
        private const string TrajectoryServiceName = "trajectory";
        private const string MultiplayerServiceName = "multiplayer";

        private const string CommandRadiallyOrient = "multiuser/radially-orient-origins";

        /// <summary>
        /// The transform that represents the box that contains the simulation.
        /// </summary>
        [SerializeField]
        private Transform simulationSpaceTransform;

        /// <summary>
        /// The transform that represents the actual simulation.
        /// </summary>
        [SerializeField]
        private Transform rightHandedSimulationSpace;

        [SerializeField]
        private InteractableScene interactableScene;

        [SerializeField]
        private NanoverImdApplication application;

        public TrajectorySession Trajectory { get; } = new TrajectorySession();
        public MultiplayerSession Multiplayer { get; } = new MultiplayerSession();

        public ParticleInteractionCollection Interactions;

        private Dictionary<string, GrpcConnection> channels
            = new Dictionary<string, GrpcConnection>();

        private NativeWebSocket.WebSocket websocket;

        /// <summary>
        /// The route through which simulation space can be manipulated with
        /// gestures to perform translation, rotation, and scaling.
        /// </summary>
        public ManipulableScenePose ManipulableSimulationSpace { get; private set; }

        /// <summary>
        /// The route through which simulated particles can be manipulated with
        /// grabs.
        /// </summary>
        public ManipulableParticles ManipulableParticles { get; private set; }

        public SynchronisedFrameSource FrameSynchronizer { get; private set; }

        public event Action ConnectionEstablished;

        public event Action<Message> OnMessage;

        /// <summary>
        /// Connect to the host address and attempt to open clients for the
        /// trajectory and multiplayer services.
        /// </summary>
        public async UniTask Connect(string address,
                                     int? trajectoryPort,
                                     int? multiplayerPort = null)
        {
            await CloseAsync();

            if (trajectoryPort.HasValue)
                Trajectory.OpenClient(GetChannel(address, trajectoryPort.Value));

            if (multiplayerPort.HasValue)
                await Multiplayer.OpenClient(GetChannel(address, multiplayerPort.Value));

            gameObject.SetActive(true);

            ConnectionEstablished?.Invoke();
        }

        public async UniTask ConnectWebSocket(string address)
        {
            if (websocket != null)
                await websocket.Close();

            var serializer = new MessagePackSerializer().WithDynamicObjectConverter();
            websocket = new NativeWebSocket.WebSocket(address);
            
            UniTask SendWebsocketMessage(Message message)
            {
                var bytes = serializer.Serialize(message, Witness.ShapeProvider);
                return websocket.Send(bytes).AsUniTask();
            }
            
            websocket.OnMessage += (bytes) =>
            {
                var message = serializer.Deserialize<Message>(bytes, Witness.ShapeProvider)!;
                OnMessage?.Invoke(message);
            };

            Trajectory.OpenClient(websocket, this);
            Multiplayer.OpenClient(websocket, this, SendWebsocketMessage);

            websocket.OnOpen += () =>
            {
                gameObject.SetActive(true);
                ConnectionEstablished?.Invoke();
            };

            websocket.OnClose += (code) => Disconnect();

            OnMessage += (Message message) =>
            {
                if (message.CommandUpdates is { } updates)
                    ReceiveWebSocketCommands(updates);
            };

            websocket.Connect().AsUniTask().Forget();
        }

        private Dictionary<int, UniTaskCompletionSource<CommandReturn>> pendingCommands = new Dictionary<int, UniTaskCompletionSource<CommandReturn>>();

        private void ReceiveWebSocketCommands(List<CommandUpdate> updates)
        {
            foreach (var update in updates)
            {
                if (pendingCommands.Remove(update.Request.Id, out var source))
                    source.TrySetResult(update.Response);
            }
        }

        private UniTask<CommandReturn> RunWebSocketCommand(string name, CommandArguments args = null)
        {
            var source = new UniTaskCompletionSource<CommandReturn>();

            var id = UnityEngine.Random.Range(0, int.MaxValue);
            pendingCommands[id] = source;

            var message = new Message
            {
                CommandUpdates = new List<CommandUpdate>
                {
                    new CommandUpdate
                    {
                        Request = new CommandRequest
                        {
                            Name = name,
                            Arguments = args,
                            Id = id,
                        }
                    }
                }
            };

            SendWebsocketMessage(message);
            return source.Task;

            UniTask SendWebsocketMessage(Message message)
            {
                var serializer = new MessagePackSerializer().WithDynamicObjectConverter();
                var bytes = serializer.Serialize(message, Witness.ShapeProvider);
                return websocket.Send(bytes).AsUniTask();
            }
        }

        private void Awake()
        {
            Interactions = new ParticleInteractionCollection(Multiplayer);
            
            ManipulableSimulationSpace = new ManipulableScenePose(simulationSpaceTransform,
                                                                  Multiplayer,
                                                                  application.CalibratedSpace);

            ManipulableParticles = new ManipulableParticles(rightHandedSimulationSpace,
                                                            Interactions,
                                                            interactableScene);

            FrameSynchronizer = gameObject.GetComponent<SynchronisedFrameSource>();
            if (FrameSynchronizer == null)
                FrameSynchronizer = gameObject.AddComponent<SynchronisedFrameSource>();
            FrameSynchronizer.FrameSource = Trajectory;

#if UNITY_EDITOR
            // Unity crashes if we don't disconnect ASAP before leaving playmode (due to YAHH http I think)
            EditorApplication.playModeStateChanged += (state) => CloseAsync().Forget();
#endif
        }

        private IEnumerator OnApplicationQuit()
        {
            yield return CloseAsync();
        }

        /// <summary>
        /// Connect to services as advertised by an ESSD service hub.
        /// </summary>
        public async UniTask Connect(ServiceHub hub)
        {
            Debug.Log($"Connecting to {hub.Name} ({hub.Id})");

            var services = hub.Properties["services"] as JObject;
            await Connect(hub.Address,
                          GetServicePort(TrajectoryServiceName),
                          GetServicePort(MultiplayerServiceName));

            int? GetServicePort(string name)
            {
                return services.ContainsKey(name) ? services[name].ToObject<int>() : null;
            }
        }

        public async UniTask Connect(DiscoveryEntry entry)
        {
            Debug.Log($"Connecting to {entry.info.name} ({entry.info.ws})");
            await ConnectWebSocket(entry.info.ws);
        }

        public async UniTask AutoConnectWebSocket()
        {
            var request = UnityWebRequest.Get("https://irl-discovery.onrender.com/list");
            await request.SendWebRequest();

            var json = request.downloadHandler.text;
            json = "{\"list\":" + json + "}";

            var listing = JsonUtility.FromJson<DiscoveryListing>(json);
            var address = listing.list[0].info.ws;

            await ConnectWebSocket(address);
        }

        /// <summary>
        /// Run an ESSD search and connect to the first service found, or none
        /// if the timeout elapses without finding a service.
        /// </summary>
        public async UniTask AutoConnect(int millisecondsTimeout = 1000)
        {
            var client = new Client();
            var services = await Task.Run(() => client.SearchForServices(millisecondsTimeout));
            if (services.Count > 0)
                await Connect(services.First());
        }

        /// <summary>
        /// Close all sessions.
        /// </summary>
        public async UniTask CloseAsync()
        {
            pendingCommands.Clear();
            ManipulableParticles.ClearAllGrabs();

            Trajectory.CloseClient();
            await Multiplayer.CloseClient();
            await websocket?.Close();

            foreach (var channel in channels.Values)
            {
                channel.Close();
            }

            channels.Clear();

            if (this != null && gameObject != null)
                gameObject.SetActive(false);
        }

        private GrpcConnection GetChannel(string address, int port)
        {
            string key = $"{address}:{port}";

            if (!channels.TryGetValue(key, out var channel))
            {
                channel = new GrpcConnection(address, port);
                channels[key] = channel;
            }

            return channel;
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
        }
        private async void OnDestroy()
        {
            if (websocket != null)
                await websocket.Close();

            await CloseAsync();
        }
        
        public void Disconnect()
        {
            CloseAsync().Forget();
        }

        public UniTask<CommandReturn> RunCommand(string command, CommandArguments arguments = null)
        {
            if (!Trajectory.Connected)
                return UniTask.FromCanceled<CommandReturn>();

            if (websocket != null)
            {
                return RunWebSocketCommand(command, arguments);
            }
            else
            {
                return Trajectory.RunCommand(command, arguments);
            }
        }

        public void PlayTrajectory()
        {
            Trajectory.Play();
        }

        public void PauseTrajectory()
        {
            Trajectory.Pause();
        }

        public void ResetTrajectory()
        {
            Trajectory.Reset();
        }

        public void StepForwardTrajectory()
        {
            Trajectory.Step();
        }


        public void StepBackwardTrajectory()
        {
            Trajectory.StepBackward();
        }

        /// <summary>
        /// Reset the box to the unit position.
        /// </summary>
        public void ResetBox()
        {
            var calibPose = application.CalibratedSpace
                                       .TransformPoseWorldToCalibrated(Transformation.Identity);
            Multiplayer.SimulationPose.UpdateValueWithLock(calibPose);
        }

        /// <summary>
        /// Run the radial orientation command on the server. This generates
        /// shared state values that suggest relative origin positions for all
        /// connected users.
        /// </summary>
        public void RunRadialOrientation()
        {
            Trajectory.RunCommand(
                CommandRadiallyOrient, 
                new Dictionary<string, object> { ["radius"] = .01 }
            );
        }
    }
}