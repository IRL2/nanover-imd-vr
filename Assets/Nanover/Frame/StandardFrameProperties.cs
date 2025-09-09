using System;
using Nanover.Core.Math;
using Nanover.Core.Science;
using UnityEngine;

namespace Nanover.Frame
{
    /// <summary>
    /// Standard names and types for <see cref="Frame"/>s.
    /// </summary>
    public static class StandardFrameProperties
    {
        public static readonly (string Key, Type Type) Bonds
            = (FrameData.BondArrayKey, typeof(BondPair[]));

        public static readonly (string Key, Type Type) BondOrders
            = (FrameData.BondOrderArrayKey, typeof(int[]));

        public static readonly (string Key, Type Type) EntityCount
            = (FrameData.ChainCountValueKey, typeof(int));

        public static readonly (string Key, Type Type) EntityName
            = (FrameData.ChainNameArrayKey, typeof(string[]));

        public static readonly (string Key, Type Type) KineticEnergy
            = (FrameData.KineticEnergyValueKey, typeof(float));

        public static readonly (string Key, Type Type) ParticleCount
            = (FrameData.ParticleCountValueKey, typeof(int));

        public static readonly (string Key, Type Type) ParticleElements
            = (FrameData.ParticleElementArrayKey, typeof(Element[]));

        public static readonly (string Key, Type Type) ParticleNames
            = (FrameData.ParticleNameArrayKey, typeof(string[]));

        public static readonly (string Key, Type Type) ParticlePositions
            = (FrameData.ParticlePositionArrayKey, typeof(Vector3[]));

        public static readonly (string Key, Type Type) ParticleResidues
            = (FrameData.ParticleResidueArrayKey, typeof(int[]));

        public static readonly (string Key, Type Type) ParticleTypes
            = (FrameData.ParticleTypeArrayKey, typeof(string[]));

        public static readonly (string Key, Type Type) PotentialEnergy
            = (FrameData.PotentialEnergyValueKey, typeof(float));

        public static readonly (string Key, Type Type) ResidueEntities
            = (FrameData.ResidueChainArrayKey, typeof(int[]));

        public static readonly (string Key, Type Type) ResidueCount
            = (FrameData.ResidueCountValueKey, typeof(int));

        public static readonly (string Key, Type Type) ResidueNames
            = (FrameData.ResidueNameArrayKey, typeof(string[]));
        
        public static readonly (string Key, Type Type) BoxTransformation
            = ("system.box.vectors", typeof(LinearTransformation));

        public static readonly (string Key, Type Type)[] All = new[]
        {
            Bonds, BondOrders, EntityCount, EntityName, KineticEnergy, ParticleCount,
            ParticleElements, ParticleNames, ParticlePositions, ParticleResidues, ParticleTypes,
            PotentialEnergy, ResidueEntities, ResidueCount, ResidueNames, BoxTransformation
        };
    }
}