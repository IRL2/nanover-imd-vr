using Essd;
using Nanover.Core.Async;
using Nanover.Core.Math;
using Nanover.Frontend.Manipulation;
using Nanover.Grpc;
using Nanover.Grpc.Multiplayer;
using Nanover.Grpc.Trajectory;
using Nanover.Visualisation;
using NanoverImd.Interaction;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NanoverImd
{
    public class NanoverImdSimulation : MonoBehaviour
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

        /// <summary>
        /// Connect to the host address and attempt to open clients for the
        /// trajectory and multiplayer services.
        /// </summary>
        public async Task Connect(string address,
                                  int? trajectoryPort,
                                  int? multiplayerPort = null)
        {
            await CloseAsync();

            Task trajectory = Task.CompletedTask;
            Task multiplayer = Task.CompletedTask;

            if (trajectoryPort.HasValue)
                trajectory = Trajectory.OpenClient(GetChannel(address, trajectoryPort.Value));
            
            if (multiplayerPort.HasValue)
                multiplayer = Multiplayer.OpenClient(GetChannel(address, multiplayerPort.Value));

            await Task.WhenAll(trajectory, multiplayer);

            gameObject.SetActive(true);

            ConnectionEstablished?.Invoke();
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
        }

        /// <summary>
        /// Connect to services as advertised by an ESSD service hub.
        /// </summary>
        public async Task Connect(ServiceHub hub)
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

        /// <summary>
        /// Run an ESSD search and connect to the first service found, or none
        /// if the timeout elapses without finding a service.
        /// </summary>
        public async Task AutoConnect(int millisecondsTimeout = 1000)
        {
            var client = new Client();
            var services = await Task.Run(() => client.SearchForServices(millisecondsTimeout));
            if (services.Count > 0)
                await Connect(services.First());
        }

        /// <summary>
        /// Close all sessions.
        /// </summary>
        public async Task CloseAsync()
        {
            ManipulableParticles.ClearAllGrabs();

            await Task.WhenAll(
                Trajectory.CloseClient(), 
                Multiplayer.CloseClient());

            foreach (var channel in channels.Values)
            {
                await channel.CloseAsync();
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


        private async void OnDestroy()
        {
            await CloseAsync();
        }
        
        public void Disconnect()
        {
            _ = CloseAsync();
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