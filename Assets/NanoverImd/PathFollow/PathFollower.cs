using System;
using System.Collections.Generic;
using System.Linq;
using NanoverImd.Interaction;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static NanoverImd.Interaction.InteractableScene;
using Random = UnityEngine.Random;

namespace NanoverImd.PathFollower
{
    public class PathFollower : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;

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
        public string Type = "spring";
        [Range(0f, 500f)]
        public float Scale = 300f;
        [Range(0f, 1f)]
        public float Speed = 0.5f;
        [Range(0f, 1f)]
        public float ErrorThreshold = 0.1f;

        private void OnEnable()
        {
            var frame = simulation.FrameSynchronizer.CurrentFrame;

            Interaction = new ParticleInteraction()
            {
                Particles = GetInteractionIndices(frame, 60, InteractionTarget.Single).ToList(),
                InteractionType = Type,
                Scale = Scale,
                MaxForce = 1000,
                MassWeighted = true,
                Other = { { "label", "automated" }, { "owner.id", simulation.Multiplayer.AccessToken } },
            };

            //RandomisePath(frame.ParticlePositions[0]);
            targetDistance = 0;

            var positions = new Vector3[testLine.positionCount];
            testLine.GetPositions(positions);
            path = positions.ToList();
            debugLine.positionCount = path.Count;
            debugLine.SetPositions(path.ToArray());

            Update();
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
            simulation.Interactions.RemoveValue(Id);
        }

        private void Update()
        {
            var frame = simulation.FrameSynchronizer.CurrentFrame;

            Interaction.InteractionType = Type;
            Interaction.Scale = Scale;

            var remaining = targetDistance;
            var local = Vector3.zero;

            for (int i = 1; i < path.Count; ++i)
            {
                var delta = path[i] - path[i - 1];
                var length = delta.magnitude;
                var direction = delta.normalized;

                if (remaining < length)
                {
                    local = path[i - 1] + direction * remaining;
                    break;
                }
                else
                {
                    remaining -= length;
                }
            }

            targetSphere.localPosition = local;
            errorSphere.localScale = Vector3.one * ErrorThreshold * 2;
            Interaction.Position = local;
            simulation.Interactions.UpdateValue(Id, Interaction);

            var centroid = computeParticleCentroid(Interaction.Particles);
            var error = Vector3.Distance(local, centroid);

            if (error < ErrorThreshold)
            {
                targetDistance += Time.deltaTime * 0.5f;
            }

            Vector3 computeParticleCentroid(IReadOnlyList<int> particleIds)
            {
                var centroid = Vector3.zero;

                for (int i = 0; i < particleIds.Count; ++i)
                    centroid += frame.ParticlePositions[particleIds[i]];

                return centroid / particleIds.Count;
            }
        }

        public void RandomisePath(Vector3 origin)
        {
            path.Clear();
            path.Add(origin);

            var rotation = Random.rotationUniform;

            for (int i = 1; i < 50; ++i)
            {
                var turn = Random.rotationUniform;
                rotation = Quaternion.Slerp(rotation, turn, .25f).normalized;
                var move = (rotation * Vector3.forward);

                path.Add(path[^1] + move * Random.value * .5f);
            }

            debugLine.positionCount = path.Count;
            debugLine.SetPositions(path.ToArray());
        }
    }
}