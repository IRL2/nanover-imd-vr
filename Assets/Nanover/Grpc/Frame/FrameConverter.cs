using System;
using System.Collections.Generic;
using System.Data;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using Nanover.Core;
using Nanover.Frame;
using Nanover.Frame.Event;
using Nanover.Protocol;
using Nanover.Protocol.Trajectory;
using UnityEngine;

namespace Nanover.Grpc.Frame
{
    /// <summary>
    /// Conversion methods for converting <see cref="FrameData" />, the standard
    /// protocol for transmitting frame information, into a <see cref="Frame" />
    /// representation used by the frontend.
    /// </summary>
    public static class FrameConverter
    {
        public static (Nanover.Frame.Frame Frame, FrameChanges Update) ConvertFrame(
            MessagePackTesting.FrameUpdate data,
            Nanover.Frame.Frame previousFrame = null)
        {
            var frame
                = previousFrame != null
                ? Nanover.Frame.Frame.ShallowCopy(previousFrame)
                : new Nanover.Frame.Frame();
            var changes = FrameChanges.None;

            if (data.ParticleCount is { } count) {
                frame.ParticleCount = count;
                changes.MarkAsChanged(FrameData.ParticleCountValueKey);
            }

            if (data.ParticlePositions is { } positions)
            {
                frame.ParticlePositions = positions;
                changes.MarkAsChanged(FrameData.ParticlePositionArrayKey);
            }

            if (data.ParticleElements is { } elements)
            {
                frame.ParticleElements = elements;
                changes.MarkAsChanged(FrameData.ParticleElementArrayKey);
            }

            if (data.ParticleNames is { } names)
            {
                frame.ParticleNames = names;
                changes.MarkAsChanged(FrameData.ParticleNameArrayKey);
            }

            if (data.ParticleResidues is { } residues) {
                frame.ParticleResidues = residues;
                changes.MarkAsChanged(FrameData.ParticleResidueArrayKey);
            }

            if (data.ParticleTypes is { } types)
            {
                frame.ParticleTypes = types;
                changes.MarkAsChanged(FrameData.ParticleTypeArrayKey);
            }

            if (data.BondPairs is { } bonds)
            {
                frame.BondPairs = bonds;
                changes.MarkAsChanged(FrameData.BondArrayKey);
            }

            if (data.BondOrders is { } orders)
            {
                frame.BondOrders = orders;
                changes.MarkAsChanged(FrameData.BondOrderArrayKey);
            }

            if (data.ResidueCount is { } residueCount)
            {
                frame.ResidueCount = residueCount;
                changes.MarkAsChanged(FrameData.ResidueCountValueKey);
            }

            if (data.ResidueNames is { } residueNames)
            {
                frame.ResidueNames = residueNames;
                changes.MarkAsChanged(FrameData.ResidueNameArrayKey);
            }

            if (data.EntityCount is { } entityCount)
            {
                frame.EntityCount = entityCount;
                changes.MarkAsChanged(FrameData.ChainCountValueKey);
            }

            if (data.EntityName is { } entityNames)
            {
                changes.MarkAsChanged(FrameData.ChainNameArrayKey);
            }

            if (data.ResidueEntities is { } residueEntities)
            {
                frame.ResidueEntities = residueEntities;
                changes.MarkAsChanged(FrameData.ResidueChainArrayKey);
            }

            if (data.BoxVectors is { } box)
            {
                frame.BoxVectors = new Core.Math.LinearTransformation(box[0], box[1], box[2]);
                changes.MarkAsChanged(StandardFrameProperties.BoxTransformation.Key);
            }

            if (data.KineticEnergy is { } kineticEnergy) {
                changes.MarkAsChanged(FrameData.KineticEnergyValueKey);
            }

            if (data.PotentialEnergy is { } potentialEnergy)
            {
                changes.MarkAsChanged(FrameData.PotentialEnergyValueKey);
            }

            return (frame, changes);
        }

        /// <summary>
        /// Convert data into a <see cref="Frame" />.
        /// </summary>
        /// <param name="previousFrame">
        /// A previous frame, from which to copy existing arrays if they exist.
        /// </param>
        public static (Nanover.Frame.Frame Frame, FrameChanges Update) ConvertFrame(
            [NotNull] FrameData data,
            [CanBeNull] Nanover.Frame.Frame previousFrame = null)
        {
            var frame = previousFrame != null
                            ? Nanover.Frame.Frame.ShallowCopy(previousFrame)
                            : new Nanover.Frame.Frame();
            var changes = FrameChanges.None;

            foreach (var (id, array) in data.Arrays)
            {
                frame.Data[id] = DeserializeArray(id, array);
                changes.MarkAsChanged(id);
            }

            foreach (var (id, value) in data.Values)
            {
                frame.Data[id] = DeserializeValue(id, value);
                changes.MarkAsChanged(id);
            }

            return (frame, changes);
        }

        /// <summary>
        /// Deserialize a protobuf <see cref="Value" /> to a C# object, using a converter
        /// if defined.
        /// </summary>
        private static object DeserializeValue(string id, Value value)
        {
            return valueConverters.TryGetValue(id, out var converter)
                       ? converter(value)
                       : value.ToObject();
        }

        /// <summary>
        /// Deserialize a protobuf <see cref="ValueArray" /> to a C# object, using a
        /// converter if defined.
        /// </summary>
        private static object DeserializeArray(string id, ValueArray array)
        {
            return arrayConverters.TryGetValue(id, out var converter)
                       ? converter(array)
                       : array.ToArray();
        }

        /// <summary>
        /// Builtin array converters for <see cref="FrameData" />
        /// </summary>
        private static readonly Dictionary<string, Converter<ValueArray, object>> arrayConverters =
            new Dictionary<string, Converter<ValueArray, object>>
            {
                [FrameData.BondArrayKey] = FrameConversions.ToBondPairArray,
                [FrameData.ParticleElementArrayKey] = FrameConversions.ToElementArray,
                [FrameData.ParticlePositionArrayKey] = Conversions.ToVector3Array,
                [StandardFrameProperties.BoxTransformation.Key] 
                = (obj) => (object) obj.ToLinearTransformation()
            };

        /// <summary>
        /// Builtin value converters for <see cref="FrameData" />
        /// </summary>
        private static readonly Dictionary<string, Converter<Value, object>> valueConverters =
            new Dictionary<string, Converter<Value, object>>
            {
                [FrameData.ParticleCountValueKey] = s => s.ToInt(),
                [FrameData.ResidueCountValueKey] = s => s.ToInt(),
                [FrameData.ChainCountValueKey] = s => s.ToInt()
            };
    }
}