using MessagePack.Formatters;
using MessagePack;
using System;
using UnityEngine;
using Nanover.Core.Science;
using Nanover.Frame;
using NanoverImd;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

[GeneratedMessagePackResolver]
public class ElementArray : IMessagePackFormatter<Element[]>
{
    public Element[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var bytes = options.Resolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, options);
        var elements = new Element[bytes.Length];

        for (int i = 0; i < bytes.Length; ++i)
            elements[i] = (Element)bytes[i];

        return elements;
    }

    public void Serialize(ref MessagePackWriter writer, Element[] value, MessagePackSerializerOptions options)
    {
        var bytes = new byte[value.Length];
        for (int i = 0; i < bytes.Length; ++i)
            bytes[i] = (byte)value[i];

        options.Resolver.GetFormatterWithVerify<byte[]>().Serialize(ref writer, bytes, options);
    }
}

[GeneratedMessagePackResolver]
public class Float32ArrayArray : IMessagePackFormatter<float[][]>
{
    public float[][] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var byteArrays = options.Resolver.GetFormatterWithVerify<byte[][]>().Deserialize(ref reader, options);
        var floatArrays = new float[byteArrays.Length][];

        for (int y = 0; y < byteArrays.Length; ++y)
        {
            var bytes = byteArrays[y];
            var floats = new float[bytes.Length / sizeof(float)];

            floatArrays[y] = floats;

            for (int x = 0; x < floatArrays[y].Length; ++x)
                floatArrays[y][x] = BitConverter.ToSingle(bytes, x * sizeof(float));
        }

        return floatArrays;
    }

    public void Serialize(ref MessagePackWriter writer, float[][] value, MessagePackSerializerOptions options)
    {
    }
}

[GeneratedMessagePackResolver]
public class Vector3ArrayArray : IMessagePackFormatter<Vector3[][]>
{
    public Vector3[][] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var byteArrays = options.Resolver.GetFormatterWithVerify<byte[][]>().Deserialize(ref reader, options);
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

    public void Serialize(ref MessagePackWriter writer, Vector3[][] value, MessagePackSerializerOptions options)
    {
    }
}

[GeneratedMessagePackResolver]
public class BondPairArray : IMessagePackFormatter<BondPair[]>
{
    public BondPair[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var bytes = options.Resolver.GetFormatterWithVerify<byte[]>().Deserialize(ref reader, options);
        var bonds = new BondPair[bytes.Length / sizeof(UInt32) / 2];

        for (int i = 0; i < bonds.Length; ++i)
        {
            bonds[i].A = (int)BitConverter.ToUInt32(bytes, (i * 2 + 0) * sizeof(UInt32));
            bonds[i].B = (int)BitConverter.ToUInt32(bytes, (i * 2 + 1) * sizeof(UInt32));
        }

        return bonds;
    }

    public void Serialize(ref MessagePackWriter writer, BondPair[] value, MessagePackSerializerOptions options)
    {
    }
}

[MessagePackObject]
public class TrajectoryData
{
    [Key("topology")]
    public TopologyData Topology;

    [Key("positions")]
    [MessagePackFormatter(typeof(Vector3ArrayArray))]
    public Vector3[][] Positions;
}

[MessagePackObject]
public class TopologyData
{
    [Key("elements")]
    [MessagePackFormatter(typeof(ElementArray))]
    public Element[] Elements;

    [Key("bonds")]
    [MessagePackFormatter(typeof(BondPairArray))]
    public BondPair[] Bonds;
}


public class MessagePackTest : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(.5f);

        byte[] bytes;

        using (UnityWebRequest www = UnityWebRequest.Get(Application.streamingAssetsPath + "/ludo-gluhut-5.msgpack"))
        {
            yield return www.SendWebRequest();

            DebugPanel.Instance.AddText($"{www.result} -- {www.downloadedBytes}\n");
            bytes = www.downloadHandler.data;
        }

        try
        {
            var obj = MessagePackSerializer.Deserialize<Dictionary<string, object>>(bytes);
            var line = gameObject.AddComponent<LineRenderer>();
            DebugPanel.Instance.AddText($"{string.Join(", ", obj.Keys)}");
            DebugPanel.Instance.AddText($"{(obj["positions"] as object[]).Length}");

            var obj2 = MessagePackSerializer.Deserialize<TrajectoryData>(bytes);
            line.positionCount = obj2.Positions[0].Length;
            line.SetPositions(obj2.Positions[0]);
            line.startWidth = .05f;
            line.endWidth = .05f;
        }
        catch (Exception e)
        {
            DebugPanel.Instance.AddText($"\n{e.Message}\n{e.InnerException}\n{e.StackTrace}\n{e.ToString()}");
        }
    }
}
