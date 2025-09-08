using Nanover.Core.Math;
using Nanover.Core.Science;
using Nanover.Frame;
using Nanover.Frame.Event;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static (Nanover.Frame.Frame frame, FrameChanges Update) ConvertFrame(
            Dictionary<string, object> data,
            Nanover.Frame.Frame previousFrame = null)
        {
            var frame = previousFrame != null
                ? Nanover.Frame.Frame.ShallowCopy(previousFrame)
                : new Nanover.Frame.Frame();
            var changes = FrameChanges.None;

            foreach (var (id, value) in data)
            {
                frame.Data[id] = DeserializeData(id, value);
                changes.MarkAsChanged(id);
            }

            return (frame, changes);
        }

        private static object DeserializeData(string id, object value)
        {
            return dataConverters.TryGetValue(id, out var converter) ? converter(value) : value;
        }

        private static readonly Dictionary<string, Converter<object, object>> dataConverters =
            new Dictionary<string, Converter<object, object>>
            {
                [FrameData.ParticleCountValueKey] = (value) => Convert.ToInt32(value),
                [FrameData.ChainCountValueKey] = (value) => Convert.ToInt32(value),
                [FrameData.ResidueCountValueKey] = (value) => Convert.ToInt32(value),

                [FrameData.ParticlePositionArrayKey] = (value) => Converters.BytesToVector3Array((byte[])value),
                [FrameData.ParticleElementArrayKey] = (value) => Converters.BytesToElementArray((byte[])value),
                [FrameData.ParticleResidueArrayKey] = (value) => Converters.BytesToUInt32((byte[])value),
                [FrameData.ResidueChainArrayKey] = (value) => Converters.BytesToUInt32((byte[])value),

                [FrameData.ParticleNameArrayKey] = (value) => ((object[])value).Cast<string>().ToArray(),
                [FrameData.ResidueNameArrayKey] = (value) => ((object[])value).Cast<string>().ToArray(),

                [FrameData.BondArrayKey] = (value) => Converters.BytesToBondPairArray((byte[])value),
                [StandardFrameProperties.BoxTransformation.Key] = (value) => Converters.BytesToLinearTransformation((byte[])value),
            };
    }

    public static class Converters
    {
        public static LinearTransformation BytesToLinearTransformation(byte[] bytes)
        {
            var axes = BytesToVector3Array(bytes);
            return new LinearTransformation(axes[0], axes[1], axes[2]);
        }

        public static UInt32[] BytesToUInt32(byte[] bytes)
        {
            var values = new UInt32[bytes.Length / sizeof(UInt32)];

            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = BitConverter.ToUInt32(bytes, i * sizeof(UInt32));
            }

            return values;
        }

        public static Vector3[] BytesToVector3Array(byte[] bytes)
        {
            var vec3 = new Vector3[bytes.Length / sizeof(float) / 3];

            for (int x = 0; x < vec3.Length; ++x)
                vec3[x] = new Vector3(
                    BitConverter.ToSingle(bytes, (x * 3 + 0) * sizeof(float)),
                    BitConverter.ToSingle(bytes, (x * 3 + 1) * sizeof(float)),
                    BitConverter.ToSingle(bytes, (x * 3 + 2) * sizeof(float))
                );

            return vec3;
        }

        public static Element[] BytesToElementArray(byte[] bytes)
        {
            var elements = new Element[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
                elements[i] = (Element)bytes[i];

            return elements;
        }

        public static BondPair[] BytesToBondPairArray(byte[] bytes)
        {
            var bonds = new BondPair[bytes.Length / sizeof(UInt32) / 2];

            for (int i = 0; i < bonds.Length; ++i)
            {
                bonds[i].A = (int)BitConverter.ToUInt32(bytes, (i * 2 + 0) * sizeof(UInt32));
                bonds[i].B = (int)BitConverter.ToUInt32(bytes, (i * 2 + 1) * sizeof(UInt32));
            }

            return bonds;
        }

        public static float[] BytesToFloatArray(byte[] bytes)
        {
            var values = new float[bytes.Length / sizeof(float)];

            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = BitConverter.ToSingle(bytes, i * sizeof(float));
            }

            return values;
        }
    }
}
