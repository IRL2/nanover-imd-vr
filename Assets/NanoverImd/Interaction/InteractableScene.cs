using System.Collections.Generic;
using System.Linq;
using Nanover.Core.Math;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.Manipulation;
using Nanover.Visualisation;
using Nanover.Visualisation.Properties;
using Nanover.Visualisation.Property;
using NanoverImd.Selection;
using UnityEngine;

namespace NanoverImd.Interaction
{
    /// <summary>
    /// Exposes a <see cref="SynchronisedFrameSource"/> that allows particles to be grabbed, accounting for the interaction method of the selections.
    /// </summary>
    public class InteractableScene : MonoBehaviour, IInteractableParticles
    {
        [Header("The provider of the frames which can be grabbed.")]
        [SerializeField]
        private SynchronisedFrameSource frameSource;

        [Header("The object which provides the selection information.")]
        [SerializeField]
        private VisualisationScene visualisationScene;

        [SerializeField]
        private NanoverImdSimulation simulation;

        [SerializeField]
        private VrController controller;

        public enum InteractionTarget
        {
            Single,
            Residue
        }

        public float InteractionRange = 0.40f;

        [SerializeField]
        private InteractionTarget interactionTarget = InteractionTarget.Single;

        public void SetInteractionTarget(InteractionTarget target)
        {
            this.interactionTarget = target;
        }

        public void SetInteractionTargetSingle()
        {
            SetInteractionTarget(InteractionTarget.Single);
            controller.PushNotification($"Target: Single");
        }
        
        public void SetInteractionTargetResidue()
        {
            SetInteractionTarget(InteractionTarget.Residue);
            controller.PushNotification($"Target: Residue");
        }

        public void SetForceType(string type)
        {
            simulation.ManipulableParticles.ForceType = type;
            controller.PushNotification($"Force: {type}");
        }

        /// <inheritdoc cref="InteractedParticles"/>
        private readonly IntArrayProperty interactedParticles = new IntArrayProperty();

        private HashSet<int> hoveredParticles = new HashSet<int>();

        /// <summary>
        /// The set of particles which are currently being interacted with.
        /// </summary>
        public IReadOnlyProperty<int[]> InteractedParticles => interactedParticles;

        private void Update()
        {
            var interactions = simulation.Interactions;
            var pts = new HashSet<int>();
            foreach (var interaction in interactions.Values)
                pts.UnionWith(interaction.Particles);

            pts.UnionWith(hoveredParticles);
            hoveredParticles.Clear();

            interactedParticles.Value = pts.ToArray();

            const string interactionRangeKey = "suggested.interaction.range";

            if (simulation.Multiplayer.GetSharedState(interactionRangeKey) is double range)
            {
                InteractionRange = (float) range;
            }
        }

        public void HoverParticleGrab(Transformation grabberPose)
        {
            if (GetNearestParticleIndex(grabberPose) is { } particleIndex)
                hoveredParticles.UnionWith(GetInteractionIndices(particleIndex));
        }

        /// <summary>
        /// Attempt to grab the nearest particle, returning null if no interaction is possible.
        /// </summary>
        /// <param name="grabberPose">The transformation of the grabbing pivot.</param>
        public ActiveParticleGrab GetParticleGrab(Transformation grabberPose)
        {
            var particleIndex = GetNearestParticleIndex(grabberPose);

            if (!particleIndex.HasValue)
                return null;
            
            var selection = visualisationScene.GetSelectionForParticle(particleIndex.Value);
            var indices = GetInteractionIndices(particleIndex.Value);

            var grab = new ActiveParticleGrab(indices);
            if (selection.Selection.ResetVelocities)
                grab.ResetVelocities = true;
            return grab;
        }

        private int? GetNearestParticleIndex(Transformation grabberPose)
        {
            var scale = 1f / Mathf.Abs(transform.lossyScale.x);

            var particleIndex = GetClosestParticleToWorldPosition(
                grabberPose.Position,
                cutoff: scale * InteractionRange,
                includeHydrogens: false
            );

            return particleIndex;
        }

        private IEnumerable<int> GetInteractionIndices(int particleIndex)
        {
            switch (interactionTarget)
            {
                case InteractionTarget.Single:
                    yield return particleIndex;
                    break;
                case InteractionTarget.Residue:
                    var frame = simulation.FrameSynchronizer.CurrentFrame;
                    if (frame.ParticleResidues == null || frame.ParticleResidues.Length == 0)
                    {
                        yield return particleIndex;
                        break;
                    }

                    var residue = frame.ParticleResidues[particleIndex];
                    if (residue == -1)
                    {
                        yield return particleIndex;
                        break;
                    }
                    for(var i = 0; i < frame.ParticleCount; i++)
                        if (frame.ParticleResidues[i] == residue)
                            yield return i;
                    break;
            }
        }

        /// <summary>
        /// Get the particle indices to select, given the nearest particle index.
        /// </summary>
        private IReadOnlyList<int> GetIndicesInSelection(VisualisationSelection selection,
                                                      int particleIndex)
        {
            switch (selection.Selection.InteractionMethod)
            {
                case ParticleSelection.InteractionMethodGroup:
                    if (selection.FilteredIndices == null)
                        return Enumerable.Range(0, frameSource.CurrentFrame.ParticleCount)
                                         .ToArray();
                    else
                        return selection.FilteredIndices.Value;
                default:
                    return new[] { particleIndex };
            }
        }

        /// <summary>
        /// Get the closest particle to a given point in world space.
        /// </summary>
        private int? GetClosestParticleToWorldPosition(Vector3 worldPosition, float cutoff = Mathf.Infinity, bool includeHydrogens = false)
        {
            var position = transform.InverseTransformPoint(worldPosition);

            if (frameSource.CurrentFrame?.ParticlePositions is not { } positions
             || frameSource.CurrentFrame?.ParticleElements is not { } elements)
                return null;

            var bestSqrDistance = cutoff * cutoff;
            int? bestParticleIndex = null;

            var interactable = new bool[positions.Length];
            visualisationScene.GetInteractable(interactable);

            for (var i = 0; i < positions.Length; ++i)
            {
                var particlePosition = positions[i];
                var sqrDistance = Vector3.SqrMagnitude(position - particlePosition);

                var isHydrogen = (elements[i] == Nanover.Core.Science.Element.Hydrogen);
                var isInteractable = interactable[i] && (includeHydrogens || !isHydrogen);

                if (isInteractable && sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestParticleIndex = i;
                }
            }

            return bestParticleIndex;
        }
    }
}