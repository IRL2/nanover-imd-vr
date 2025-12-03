using Cysharp.Threading.Tasks;
using Essd;
using WebSocketTypes;
using Nanover.Core.Math;
using Nanover.Frontend.Manipulation;
using Nanover.Network.Multiplayer;
using Nanover.Network.Trajectory;
using Nanover.Visualisation;
using NanoverImd.Interaction;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Nerdbank.MessagePack;

using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;
using WebDiscovery;
using NativeWebSocket;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NanoverImd
{
    public class NanoverImdSimulation : MonoBehaviour, WebSocketMessageSource
    {
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

        public bool Connected => websocket?.State == WebSocketState.Open;

        private WebSocket websocket;

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
        public event Action ConnectionClosed;

        public event Action<Message> OnMessage;

        private MessagePackSerializer serializer;

        private UniTask SendWebsocketMessage(Message message)
        {
            if (!Connected)
            {
                return UniTask.FromCanceled();
            }

            var bytes = serializer.Serialize(message, Witness.ShapeProvider);
            return websocket.Send(bytes).AsUniTask();
        }

        private void OnClose(WebSocketCloseCode code)
        {
            Close();
        }

        public void ConnectWebSocket(string address)
        {
            Close();

            websocket = new WebSocket(address);
            
            websocket.OnMessage += (bytes) =>
            {
                var message = serializer.Deserialize<Message>(bytes, Witness.ShapeProvider)!;
                OnMessage?.Invoke(message);
            };

            Trajectory.OpenClient(this);
            Multiplayer.OpenClient(this, SendWebsocketMessage);

            websocket.OnOpen += () =>
            {
                gameObject.SetActive(true);
                ConnectionEstablished?.Invoke();
            };

            websocket.OnClose += OnClose;

            OnMessage += (Message message) =>
            {
                if (message.CommandUpdate is { } update)
                    ReceiveWebSocketCommand(update);
            };

            websocket.Connect().AsUniTask().Forget();
        }

        private Dictionary<int, UniTaskCompletionSource<CommandReturn>> pendingCommands = new Dictionary<int, UniTaskCompletionSource<CommandReturn>>();

        private void ReceiveWebSocketCommand(CommandUpdate update)
        {
            if (pendingCommands.Remove(update.Request.Id, out var source))
                source.TrySetResult(update.Response.StringifyStructureKeys() as CommandArguments);
        }

        private UniTask<CommandReturn> RunWebSocketCommand(string name, CommandArguments args = null)
        {
            var source = new UniTaskCompletionSource<CommandReturn>();

            var id = UnityEngine.Random.Range(0, int.MaxValue);
            pendingCommands[id] = source;

            var message = new Message
            {
                CommandUpdate = new CommandUpdate
                {
                    Request = new CommandRequest
                    {
                        Name = name,
                        Arguments = args,
                        Id = id,
                    },
                }
            };

            SendWebsocketMessage(message);
            return source.Task;
        }

        private void Awake()
        {
            serializer = new MessagePackSerializer().WithDynamicObjectConverter();

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
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                    Close();
            };
#endif
        }

        private void OnApplicationQuit()
        {
            Close();
        }

        /// <summary>
        /// Connect to services as advertised by an ESSD service hub.
        /// </summary>
        public void Connect(ServiceHub hub)
        {
            Debug.Log($"Connecting to {hub.Name} ({hub.Id})");
            var services = hub.Properties["services"] as JObject;

            if (GetServicePort("ws") is int port)
            {
                var address = $"ws://{hub.Address}:{port}";
                ConnectWebSocket(address);
            }
            else
            {
                throw new Exception("NO SERVER!");
            }

            int? GetServicePort(string name)
            {
                return services.ContainsKey(name) ? services[name].ToObject<int>() : null;
            }
        }

        public void Connect(DiscoveryEntry entry)
        {
            Debug.Log($"Connecting to {entry.info.name} ({entry.info.ws})");
            ConnectWebSocket(entry.info.ws);
        }

        public async UniTask AutoConnectWebSocket()
        {
            var request = UnityWebRequest.Get("https://irl-discovery.onrender.com/list");
            await request.SendWebRequest();

            var json = request.downloadHandler.text;
            json = "{\"list\":" + json + "}";

            var listing = JsonUtility.FromJson<DiscoveryListing>(json);
            var address = listing.list[0].info.ws;

            ConnectWebSocket(address);
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
                Connect(services.First());
        }

        /// <summary>
        /// Close all sessions.
        /// </summary>
        public void Close()
        { 
            if (this.websocket == null)
                return;

            var websocket = this.websocket;
            this.websocket = null;

            websocket.OnClose -= OnClose;

            Debug.LogError($"TRY CLOSING {websocket}");
            websocket?.Close().AsUniTask().Forget();

            gameObject.SetActive(false);

            OnMessage = null;

            pendingCommands.Clear();
            ManipulableParticles.ClearAllGrabs();

            Trajectory.CloseClient();
            Multiplayer.CloseClient();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
        }
        private void OnDestroy()
        {
            Close();
        }
        
        public void Disconnect()
        {
            Close();
        }

        public UniTask<CommandReturn> RunCommand(string command, CommandArguments arguments = null)
        {
            if (!Connected)
                return UniTask.FromCanceled<CommandReturn>();

            return RunWebSocketCommand(command, arguments);
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
            RunCommand(
                CommandRadiallyOrient, 
                new Dictionary<string, object> { ["radius"] = .01 }
            );
        }
    }
}