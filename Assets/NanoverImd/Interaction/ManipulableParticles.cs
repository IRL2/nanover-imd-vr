using System.Collections.Generic;
using System.Linq;
using Nanover.Core.Math;
using NanoverImd.Interaction;
using UnityEngine;

namespace Nanover.Frontend.Manipulation
{
    /// <summary>
    /// Situates and IMD simulation in a Unity transform and allows grab
    /// manipulations to begin and end particle interactions.
    /// </summary>
    public class ManipulableParticles
    {
        /// <summary>
        /// The force multiplier.
        /// </summary>
        public float ForceScale { get; set; } = 100f;
        public string ForceType { get; set; } = "spring";

        private readonly Transform transform;
        private readonly ParticleInteractionCollection interactions;

        public IInteractableParticles InteractableParticles { get; set; }

        private readonly HashSet<ActiveParticleGrab> activeGrabs
            = new HashSet<ActiveParticleGrab>();

        public ManipulableParticles(Transform transform,
                                    ParticleInteractionCollection interactions,
                                    IInteractableParticles interactableParticles)
        {
            this.transform = transform;
            this.interactions = interactions;
            this.InteractableParticles = interactableParticles;
        }

        public void ClearAllGrabs()
        {
            foreach (var grab in activeGrabs.ToList())
            {
                EndParticleGrab(grab);
            }
        }

        /// <summary>
        /// Start a particle grab on whatever particle falls close to the position
        /// of the given grabber. Return either the manipulation or null if there was
        /// nothing grabbable.
        /// </summary>
        public ActiveParticleGrab StartParticleGrab(UnitScaleTransformation grabberPose)
        {
            if (InteractableParticles.GetParticleGrab(grabberPose) is ActiveParticleGrab grab)
                return StartParticleGrab(grabberPose, grab);
            return null;
        }

        public void HoverParticleGrab(UnitScaleTransformation grabberPose)
        {
            InteractableParticles.HoverParticleGrab(grabberPose);
        }

        private ActiveParticleGrab StartParticleGrab(UnitScaleTransformation grabberPose,
                                                     ActiveParticleGrab grab)
        {
            grab.UpdateManipulatorPose(grabberPose);
            grab.ParticleGrabUpdated += () => OnParticleGrabUpdated(grab);
            grab.ManipulationEnded += () => EndParticleGrab(grab);
            grab.Label = "TEST";
            OnParticleGrabUpdated(grab);
            activeGrabs.Add(grab);
            return grab;
        }

        private void OnParticleGrabUpdated(ActiveParticleGrab grab)
        {
            var position = transform.InverseTransformPoint(grab.GrabPosition);

            var other = new Dictionary<string, object>();
            if (grab.OwnerId != null)
                other["owner.id"] = grab.OwnerId;
            if (grab.Label != null)
                other["label"] = grab.Label;

            interactions.UpdateValue(grab.Id, new ParticleInteraction()
            {
                Particles = grab.ParticleIndices.ToList(),
                Position = position,
                Scale = ForceScale,
                InteractionType = ForceType,
                ResetVelocities = grab.ResetVelocities,
                Other = other.Count > 0 ? other : null,
            });
        }

        private void EndParticleGrab(ActiveParticleGrab grab)
        {
            activeGrabs.Remove(grab);
            interactions.RemoveValue(grab.Id);
        }
    }
}