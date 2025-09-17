using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NanoverImd.PathFollower
{
    public class ParticleRelativeSpace : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;

        [SerializeField]
        private Transform referenceSphereTemplate;
#pragma warning restore 0649

        public List<uint> particleIds = new List<uint>(); 
        
        private List<Transform> referenceSpheres = new List<Transform>();

        private Transform SimulationSpace => transform.parent;
        private Transform PathSpace => transform;

        public Vector3 PositionFromSimulationToPath(Vector3 position) => PathSpace.InverseTransformPoint(SimulationSpace.TransformPoint(position));
        public Vector3 PositionFromPathToSimulation(Vector3 position) => SimulationSpace.InverseTransformPoint(PathSpace.TransformPoint(position));

        private void Awake()
        {
            for (int i = 0; i < 3; ++i)
            {
                var sphere = Instantiate(referenceSphereTemplate, referenceSphereTemplate.parent);
                referenceSpheres.Add(sphere);
            }
        }

        private void OnDisable()
        {
            ResetPose();

            foreach (var sphere in referenceSpheres)
            {
                sphere.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            UpdateSuggestedParameters();
            UpdatePose();

            UpdateReferenceSpheres();
        }

        private void ResetPose()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void UpdatePose()
        {
            var frame = simulation.FrameSynchronizer.CurrentFrame;

            if (particleIds.Count < 3 || frame == null)
            {
                ResetPose();
                return;
            }

            var A = frame.ParticlePositions[particleIds[0]];
            var B = frame.ParticlePositions[particleIds[1]];
            var C = frame.ParticlePositions[particleIds[2]];

            var ABu = (B - A).normalized;
            var ACu = (C - A).normalized;
            var x = ABu;
            var y = Vector3.Cross(ACu, ABu).normalized;
            var z = Vector3.Cross(y, x).normalized;

            var center = (A + B + C) / 3f;

            transform.localPosition = center;
            transform.localRotation = Quaternion.LookRotation(z, y);
        }

        private void UpdateSuggestedParameters()
        {
            const string particlesKey = "suggested.reference.particles";

            if (simulation.Multiplayer.GetSharedState(particlesKey) is IList<object> ids)
            {
                particleIds = ids.Select(id => Convert.ToUInt32(id)).ToList();
            }
            else
            {
                particleIds = new List<uint>();
            }
        }

        private void UpdateReferenceSpheres()
        {
            var frame = simulation.FrameSynchronizer.CurrentFrame;
            var count = Mathf.Min(particleIds.Count, referenceSpheres.Count);

            if (frame == null) return;

            foreach (var sphere in referenceSpheres)
            {
                sphere.gameObject.SetActive(false);
            }

            for (int i = 0; i < count; ++i)
            {
                referenceSpheres[i].gameObject.SetActive(true);
                referenceSpheres[i].localPosition = frame.ParticlePositions[particleIds[i]];
            }
        }
    }
}