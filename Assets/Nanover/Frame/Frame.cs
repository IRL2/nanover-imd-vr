using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nanover.Core;
using Nanover.Core.Math;
using Nanover.Core.Science;
using Nanover.Protocol.Trajectory;
using UnityEngine;

namespace Nanover.Frame
{
    /// <summary>
    /// A single Frame in a trajectory. It is a snapshot of a system, represented by a
    /// set of properties. These properties include particle positions, types and
    /// bonds.
    /// </summary>
    public class Frame : IFrame
    {
        /// <summary>
        /// Internal representation of an IParticle list by references arrays
        /// </summary>
        [NotNull]
        private readonly ParticleReferenceList particles;

        public Frame()
        {
            particles = new ParticleReferenceList(this);
        }

        /// <summary>
        /// Array of particle positions
        /// </summary>
        public Vector3[] ParticlePositions
        {
            get => Data.GetValueOrDefault<Vector3[]>(FrameData.ParticlePositionArrayKey);
            set => Data[FrameData.ParticlePositionArrayKey] = value;
        }

        /// <summary>
        /// Array of particle types
        /// </summary>
        public string[] ParticleTypes
        {
            get => Data.GetValueOrDefault<string[]>(FrameData.ParticleTypeArrayKey);
            set => Data[FrameData.ParticleTypeArrayKey] = value;
        }

        /// <summary>
        /// Array of particle elements
        /// </summary>
        public Element[] ParticleElements
        {
            get => Data.GetValueOrDefault<Element[]>(FrameData.ParticleElementArrayKey);
            set => Data[FrameData.ParticleElementArrayKey] = value;
        }

        /// <summary>
        /// Array of bond pairs
        /// </summary>
        public BondPair[] BondPairs
        {
            get => Data.GetValueOrDefault<BondPair[]>(FrameData.BondArrayKey);
            set => Data[FrameData.BondArrayKey] = value;
        }

        /// <summary>
        /// Array of bond pairs
        /// </summary>
        public int[] BondOrders
        {
            get => Data.GetValueOrDefault<int[]>(FrameData.BondOrderArrayKey);
            set => Data[FrameData.BondOrderArrayKey] = value;
        }

        /// <summary>
        /// Array of residue indices for each particle.
        /// </summary>
        public int[] ParticleResidues
        {
            get => Data.GetValueOrDefault<int[]>(FrameData.ParticleResidueArrayKey);
            set => Data[FrameData.ParticleResidueArrayKey] = value;
        }

        /// <summary>
        /// Array of residue names.
        /// </summary>
        public string[] ResidueNames
        {
            get => Data.GetValueOrDefault<string[]>(FrameData.ResidueNameArrayKey);
            set => Data[FrameData.ResidueNameArrayKey] = value;
        }
        
        /// <summary>
        /// Array of entity indices for each residue.
        /// </summary>
        /// <remarks>
        /// Entities are groupings of residues, such as polypeptide chains or strands of DNA.
        /// </remarks>
        public int[] ResidueEntities
        {
            get => Data.GetValueOrDefault<int[]>(FrameData.ResidueChainArrayKey);
            set => Data[FrameData.ResidueChainArrayKey] = value;
        }

        /// <summary>
        /// Array of particle names.
        /// </summary>
        public string[] ParticleNames
        {
            get => Data.GetValueOrDefault<string[]>(FrameData.ParticleNameArrayKey);
            set => Data[FrameData.ParticleNameArrayKey] = value;
        }
        
        /// <summary>
        /// The number of particles.
        /// </summary>
        public int ParticleCount
        {
            get => Data.GetIntOrZero(FrameData.ParticleCountValueKey);
            set => Data[FrameData.ParticleCountValueKey] = value;
        }
        
        /// <summary>
        /// The transformation that represents the unit cell.
        /// </summary>
        public LinearTransformation? BoxVectors
        {
            get => Data.GetValueOrDefault<LinearTransformation>(StandardFrameProperties.BoxTransformation.Key);
            set => Data[StandardFrameProperties.BoxTransformation.Key] = value;
        }
        
        /// <summary>
        /// The number of residues.
        /// </summary>
        public int ResidueCount
        {
            get => Data.GetIntOrZero(FrameData.ResidueCountValueKey);
            set => Data[FrameData.ResidueCountValueKey] = value;
        }
        
        /// <summary>
        /// The number of entities.
        /// </summary>
        public int EntityCount
        {
            get => Data.GetIntOrZero(FrameData.ChainCountValueKey);
            set => Data[FrameData.ChainCountValueKey] = value;
        }

        /// <inheritdoc />
        [NotNull]
        public IReadOnlyList<IParticle> Particles => particles;

        /// <inheritdoc />
        [NotNull]
        public IReadOnlyList<BondPair> Bonds => BondPairs ?? new BondPair[0];

        /// <inheritdoc />
        public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Obtain a copy of a frame, referencing the old arrays such that if a new array
        /// is not provided, a Frame will use the last obtained value.
        /// </summary>
        public static Frame ShallowCopy([NotNull] Frame originalFrame)
        {
            var copiedFrame = new Frame();
            foreach(var (key, value) in originalFrame.Data)
                copiedFrame.Data[key] = value;
            return copiedFrame;
        }
    }
}