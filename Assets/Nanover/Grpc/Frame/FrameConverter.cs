using System;
using System.Collections.Generic;
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

            if (data.Positions is { } positions)
            {
                frame.ParticlePositions = positions;
                changes.MarkAsChanged(FrameData.ParticlePositionArrayKey);
            }

            if (data.Elements is { } elements)
            {
                frame.ParticleElements = elements;
                changes.MarkAsChanged(FrameData.ParticleElementArrayKey);
            }

            if (data.Bonds is { } bonds)
            {
                frame.BondPairs = bonds;
                changes.MarkAsChanged(FrameData.BondArrayKey);
            }

            if (data.BoxVectors is { } box)
            {
                frame.BoxVectors = new Core.Math.LinearTransformation(box[0], box[1], box[2]);
                changes.MarkAsChanged(StandardFrameProperties.BoxTransformation.Key);
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