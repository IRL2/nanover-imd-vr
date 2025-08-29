using Cysharp.Threading.Tasks;
using Nanover.Core.Math;
using Nanover.Core.Science;
using Nanover.Frame;
using Nanover.Protocol.Trajectory;
using NativeWebSocket;
using Nerdbank.MessagePack;
using PolyType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CommandArguments = System.Collections.Generic.Dictionary<string, object>;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

namespace MessagePackTesting
{
    public class Uint32Array : MessagePackConverter<int[]>
    {
        public override int[]? Read(ref MessagePackReader reader, SerializationContext context)
        {
            var bytes = context.GetConverter<byte[]>(Witness.ShapeProvider).Read(ref reader, context);

            if (bytes is null)
                return null;

            var values = new int[bytes.Length / sizeof(UInt32)];

            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = (int)BitConverter.ToUInt32(bytes, i * sizeof(UInt32));
            }

            return values;
        }

        public override void Write(ref MessagePackWriter writer, in int[]? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var bytes = new byte[value.Length * sizeof(UInt32)];
            for (int i = 0; i < value.Length; ++i)
            {
                BitConverter.GetBytes(value[i]).CopyTo(bytes, i * sizeof(UInt32));
            }

            context.GetConverter<byte[]>(Witness.ShapeProvider).Write(ref writer, bytes, context);
        }
    }

    public class ElementArray : MessagePackConverter<Element[]>
    {
        public override Element[]? Read(ref MessagePackReader reader, SerializationContext context)
        {
            var bytes = context.GetConverter<byte[]>(Witness.ShapeProvider).Read(ref reader, context);
            if (bytes is null)
                return null;

            var elements = new Element[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
                elements[i] = (Element)bytes[i];

            return elements;
        }

        public override void Write(ref MessagePackWriter writer, in Element[]? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var bytes = new byte[value.Length];
            for (int i = 0; i < bytes.Length; ++i)
                bytes[i] = (byte)value[i];

            context.GetConverter<byte[]>(Witness.ShapeProvider).Write(ref writer, bytes, context);
        }
    }

    public class BondPairArray : MessagePackConverter<BondPair[]>
    {
        public override BondPair[]? Read(ref MessagePackReader reader, SerializationContext context)
        {
            var bytes = context.GetConverter<byte[]>(Witness.ShapeProvider).Read(ref reader, context);

            if (bytes is null)
                return null;

            var bonds = new BondPair[bytes.Length / sizeof(UInt32) / 2];

            for (int i = 0; i < bonds.Length; ++i)
            {
                bonds[i].A = (int)BitConverter.ToUInt32(bytes, (i * 2 + 0) * sizeof(UInt32));
                bonds[i].B = (int)BitConverter.ToUInt32(bytes, (i * 2 + 1) * sizeof(UInt32));
            }

            return bonds;
        }

        public override void Write(ref MessagePackWriter writer, in BondPair[]? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var bytes = new byte[value.Length * sizeof(UInt32) * 2];
            for (int i = 0; i < value.Length; ++i)
            {
                BitConverter.GetBytes(value[i].A).CopyTo(bytes, (i * 2 + 0) * sizeof(UInt32));
                BitConverter.GetBytes(value[i].B).CopyTo(bytes, (i * 2 + 1) * sizeof(UInt32));
            }

            context.GetConverter<byte[]>(Witness.ShapeProvider).Write(ref writer, bytes, context);
        }
    }

    public class Vector3ArrayArray : MessagePackConverter<Vector3[][]>
    {
        public override Vector3[][]? Read(ref MessagePackReader reader, SerializationContext context)
        {
            var byteArrays = context.GetConverter<byte[][]>(Witness.ShapeProvider).Read(ref reader, context);

            if (byteArrays is null)
                return null;

            var vec3Arrays = new Vector3[byteArrays.Length][];

            for (int y = 0; y < byteArrays.Length; ++y)
            {
                var bytes = byteArrays[y];
                var vec3 = new Vector3[bytes.Length / sizeof(float) / 3];

                vec3Arrays[y] = vec3;

                for (int x = 0; x < vec3Arrays[y].Length; ++x)
                    vec3Arrays[y][x] = new Vector3(
                        BitConverter.ToSingle(bytes, (x * 3 + 0) * sizeof(float)),
                        BitConverter.ToSingle(bytes, (x * 3 + 1) * sizeof(float)),
                        BitConverter.ToSingle(bytes, (x * 3 + 2) * sizeof(float))
                    );
            }

            return vec3Arrays;
        }

        public override void Write(ref MessagePackWriter writer, in Vector3[][]? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var byteArrays = new byte[value.Length][];

            for (int y = 0; y < byteArrays.Length; ++y)
            {
                var vec3s = value[y];
                var bytes = new byte[vec3s.Length * sizeof(float) * 3];

                byteArrays[y] = bytes;

                for (int x = 0; x < vec3s.Length; ++x)
                {
                    BitConverter.GetBytes(vec3s[x].x).CopyTo(bytes, (x * 3 + 0) * sizeof(float));
                    BitConverter.GetBytes(vec3s[x].y).CopyTo(bytes, (x * 3 + 1) * sizeof(float));
                    BitConverter.GetBytes(vec3s[x].z).CopyTo(bytes, (x * 3 + 2) * sizeof(float));
                }
            }

            context.GetConverter<byte[][]>(Witness.ShapeProvider).Write(ref writer, byteArrays, context);
        }
    }

    public class Vector3Array : MessagePackConverter<Vector3[]>
    {
        public override Vector3[]? Read(ref MessagePackReader reader, SerializationContext context)
        {
            var bytes = context.GetConverter<byte[]>(Witness.ShapeProvider).Read(ref reader, context);

            if (bytes is null)
                return null;

            var vec3 = new Vector3[bytes.Length / sizeof(float) / 3];

            for (int x = 0; x < vec3.Length; ++x)
                vec3[x] = new Vector3(
                    BitConverter.ToSingle(bytes, (x * 3 + 0) * sizeof(float)),
                    BitConverter.ToSingle(bytes, (x * 3 + 1) * sizeof(float)),
                    BitConverter.ToSingle(bytes, (x * 3 + 2) * sizeof(float))
                );

            return vec3;
        }

        public override void Write(ref MessagePackWriter writer, in Vector3[]? value, SerializationContext context)
        {
        }
    }

    public class Trajectory
    {
        [PropertyShape(Name = "topology")]
        public Topology Topology;

        [PropertyShape(Name = "positions")]
        [MessagePackConverter(typeof(Vector3ArrayArray))]
        public Vector3[][] Positions;
    }

    public class Topology
    {
         [PropertyShape(Name = "elements")]
         [MessagePackConverter(typeof(ElementArray))]
         public Element[] Elements;
 
         [PropertyShape(Name = "bonds")]
         [MessagePackConverter(typeof(BondPairArray))]
         public BondPair[] Bonds;
    }

    public class StateUpdate
    {
        [PropertyShape(Name = "updates")]
        public Dictionary<string, object> Updates = new Dictionary<string, object>();

        [PropertyShape(Name = "removals")]
        public HashSet<string> Removals = new HashSet<string>();
    }


    public partial class FrameUpdate
    {
        [PropertyShape(Name = FrameData.ParticleCountValueKey)]
        public int? ParticleCount;

        [PropertyShape(Name = FrameData.ParticleElementArrayKey)]
        [MessagePackConverter(typeof(ElementArray))]
        public Element[]? ParticleElements;

        [PropertyShape(Name = FrameData.ParticlePositionArrayKey)]
        [MessagePackConverter(typeof(Vector3Array))]
        public Vector3[]? ParticlePositions;

        [PropertyShape(Name = FrameData.ParticleNameArrayKey)]
        public string[]? ParticleNames;

        [PropertyShape(Name = FrameData.ParticleResidueArrayKey)]
        [MessagePackConverter(typeof(Uint32Array))]
        public int[]? ParticleResidues;

        [PropertyShape(Name = FrameData.ParticleTypeArrayKey)]
        public string[]? ParticleTypes;

        [PropertyShape(Name = FrameData.BondArrayKey)]
        [MessagePackConverter(typeof(BondPairArray))]
        public BondPair[]? BondPairs;

        [PropertyShape(Name = FrameData.BondOrderArrayKey)]
        public int[]? BondOrders;

        [PropertyShape(Name = FrameData.ResidueCountValueKey)]
        public int? ResidueCount;

        [PropertyShape(Name = FrameData.ResidueNameArrayKey)]
        public string[]? ResidueNames;

        [PropertyShape(Name = FrameData.ChainCountValueKey)]
        public int? EntityCount;

        [PropertyShape(Name = FrameData.ChainNameArrayKey)]
        public string[]? EntityName;

        [PropertyShape(Name = FrameData.ResidueChainArrayKey)]
        [MessagePackConverter(typeof(Uint32Array))]
        public int[]? ResidueEntities;

        [PropertyShape(Name = FrameData.KineticEnergyValueKey)]
        public float? KineticEnergy;

        [PropertyShape(Name = FrameData.PotentialEnergyValueKey)]
        public float? PotentialEnergy;

        [PropertyShape(Name = "system.box.vectors")]
        [MessagePackConverter(typeof(Vector3Array))]
        public Vector3[]? BoxVectors;
    }

    public partial class CommandRequest
    {
        [PropertyShape(Name = "id")]
        public int Id;

        [PropertyShape(Name = "name")]
        public string? Name;

        [PropertyShape(Name = "arguments")]
        public CommandArguments? Arguments;
    }

    public partial class CommandUpdate
    {
        [PropertyShape(Name = "request")]
        public CommandRequest Request;

        [PropertyShape(Name = "response")]
        public CommandReturn Response;
    }

    public partial class Message
    {
        [PropertyShape(Name = "frame")]
        public FrameUpdate? FrameUpdate;

        [PropertyShape(Name = "state")]
        public StateUpdate? StateUpdate;

        [PropertyShape(Name = "command")]
        public List<CommandUpdate>? CommandUpdates;
    }

    public interface WebSocketMessageSource
    {
        UniTask<CommandReturn> RunCommand(string name, CommandArguments args = null);

        event Action<Message> OnMessage;
    }

    [GenerateShapeFor(typeof(byte[]))]
    [GenerateShapeFor(typeof(byte[][]))]
    [GenerateShapeFor(typeof(object[]))]
    [GenerateShapeFor(typeof(HashSet<string>))]
    [GenerateShapeFor(typeof(List<object>))]
    [GenerateShapeFor(typeof(Message))]
    [GenerateShapeFor(typeof(Trajectory))]
    public partial class Witness { }

    [Serializable]
    public class DiscoveryInfo
    {
        public string name;
        public string https;
        public string ws;
    }

    [Serializable]
    public class DiscoveryEntry
    {
        public string code;
        public DiscoveryInfo info;
    }
    
    [Serializable]
    public class DiscoveryListing
    {
        public List<DiscoveryEntry> list;
    }

    public static class WebsocketDiscovery
    {
        public static readonly string Endpoint = "https://irl-discovery.onrender.com/list";

        public static async UniTask<List<DiscoveryEntry>> DiscoverWebsocketServers()
        {
            var request = UnityWebRequest.Get(Endpoint);
            await request.SendWebRequest();

            var json = request.downloadHandler.text;
            json = "{\"list\":" + json + "}";

            var listing = JsonUtility.FromJson<DiscoveryListing>(json);
            return listing.list;
        }
    }

    public static class ObjectExtensions
    {
        public static object StringifyStructureKeys(this object structure)
        {
            if (structure is not IDictionary<object, object> dict)
                return structure;

            return dict.ToDictionary(pair => pair.Key.ToString(), pair => StringifyStructureKeys(pair.Value));
        }
    }
}