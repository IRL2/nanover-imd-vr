using System;
using System.Collections.Generic;
using System.Linq;
using Nanover.Frame;
using Nanover.Frame.Event;
using NanoverImd.Interaction;
using UnityEngine;
using static NanoverImd.Interaction.InteractableScene;

namespace NanoverImd.PathFollower
{
    public class PathFollower : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;
        [SerializeField]
        private ParticleRelativeSpace pathSpace;

        [SerializeField]
        private LineRenderer debugLine;

        [SerializeField]
        private Transform targetSphere;
        [SerializeField]
        private Transform errorSphere;
#pragma warning restore 0649

        [SerializeField]
        private List<Vector3> path = new List<Vector3>();

        public LineRenderer testLine;

        private float targetDistance = 0;

        public string Id { get; } = Guid.NewGuid().ToString();
        public ParticleInteraction Interaction { get; private set; } = new ParticleInteraction();

        [Header("Params")]
        public int AtomId = 0;
        public string Type = "spring";
        public InteractionTarget Target = InteractionTarget.Residue;
        [Range(100f, 5000f)]
        public float Scale = 1000f;
        [Range(0f, 1f)]
        public float Speed = .5f;
        [Range(0f, 1f)]
        public float ErrorThreshold = 0f;

        private bool frameUpdated = false;
        private float frameDeltaTime = 0;
        private float prevFrameTime = -1f;

        public float LengthFollowed => targetDistance;

        public void Reset()
        {
            testLine = null;
            path.Clear();
            targetDistance = 0;
            AtomId = 0;
            prevFrameTime = -1f;
            frameDeltaTime = 0;
        }

        private void OnFrameUpdated(IFrame frame, FrameChanges changes)
        {
            frameUpdated = changes.HasChanged("particle.positions");
            var nextFrameTime = (float)(double)simulation.FrameSynchronizer.CurrentFrame.Data["system.simulation.time"];
            frameDeltaTime = prevFrameTime > 0 ? nextFrameTime - prevFrameTime : 0f;
            prevFrameTime = nextFrameTime;
        }

        private void OnEnable()
        {
            simulation.FrameSynchronizer.FrameChanged += OnFrameUpdated;

            var frame = simulation.FrameSynchronizer.CurrentFrame;

            Interaction = new ParticleInteraction()
            {
                Particles = GetInteractionIndices(frame, AtomId, Target).ToList(),
                InteractionType = Type,
                Scale = Scale,
                MaxForce = 1000,
                MassWeighted = true,
                Other = { { "label", "automated" }, { "owner.id", simulation.Multiplayer.AccessToken } },
            };

            targetDistance = 0;

            var positions = new Vector3[testLine.positionCount];
            testLine.GetPositions(positions);
            path = positions.ToList();
            debugLine.enabled = true;
            debugLine.positionCount = path.Count;
            debugLine.SetPositions(path.ToArray());

            Update();

            targetSphere.gameObject.SetActive(true);
        }

        public void SetPath(List<Vector3> path)
        {
            this.path = path;
            debugLine.positionCount = path.Count;
            debugLine.SetPositions(path.ToArray());
            targetDistance = 0;

            this.enabled = true;
        }

        private void OnDisable()
        {
            simulation.FrameSynchronizer.FrameChanged -= OnFrameUpdated;

            Debug.Log($"PathFollower: Disabling follower for interaction {Id}.");
            simulation.Interactions.RemoveValue(Id);
            debugLine.enabled = false;

            targetSphere.gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdateSuggestedParameters();

            var frame = simulation.FrameSynchronizer.CurrentFrame;

            Interaction.InteractionType = Type;
            Interaction.Scale = Scale;

            var remaining = targetDistance;
            var positionInPath = Vector3.zero;
            var success = false;

            for (int i = 1; i < path.Count; ++i)
            {
                var delta = path[i] - path[i - 1];
                var length = delta.magnitude;
                var direction = delta.normalized;

                if (remaining < length)
                {
                    positionInPath = path[i - 1] + direction * remaining;
                    success = true;
                    break;
                }
                else
                {
                    remaining -= length;
                }
            }

            if (!success)
            {
                enabled = false;
                return;
            }

            targetSphere.localPosition = positionInPath;
            errorSphere.localScale = Vector3.one * ErrorThreshold * 2;
            Interaction.Position = pathSpace.PositionFromPathToSimulation(positionInPath);
            simulation.Interactions.UpdateValue(Id, Interaction);

            var centroid = pathSpace.PositionFromSimulationToPath(computeParticleCentroid(Interaction.Particles));
            var error = Vector3.Distance(positionInPath, centroid);
            var inRange = (error < ErrorThreshold || ErrorThreshold == 0);

            if (inRange && frameUpdated)
            {
                targetDistance += frameDeltaTime * Speed;
            }
            frameUpdated = false;
            frameDeltaTime = 0;

            Vector3 computeParticleCentroid(IReadOnlyList<int> particleIds)
            {
                var centroid = Vector3.zero;

                for (int i = 0; i < particleIds.Count; ++i)
                    centroid += frame.ParticlePositions[particleIds[i]];

                return centroid / particleIds.Count;
            }

        }

        private void UpdateSuggestedParameters()
        {
            const string atomKey = "suggested.follower.atom";
            const string scaleKey = "suggested.follower.scale";
            const string typeKey = "suggested.follower.type";
            const string targetKey = "suggested.follower.target";
            const string speedKey = "suggested.follower.speed";
            const string thresholdKey = "suggested.follower.error";

            if (simulation.Multiplayer.GetSharedState(atomKey) is double atom)
            {
                AtomId = (int) atom;
            }

            if (simulation.Multiplayer.GetSharedState(scaleKey) is double scale)
            {
                Scale = (float) scale;
            }

            if (simulation.Multiplayer.GetSharedState(typeKey) is string type)
            {
                Type = type;
            }

            if (simulation.Multiplayer.GetSharedState(targetKey) is string target)
            {
                Target = target == "single" ? InteractionTarget.Single : InteractionTarget.Residue;
            }

            if (simulation.Multiplayer.GetSharedState(speedKey) is double speed)
            {
                Speed = (float) speed;
            }

            if (simulation.Multiplayer.GetSharedState(thresholdKey) is double threshold)
            {
                ErrorThreshold = (float) threshold;
            }
        }
    }
}