using MessagePack.Formatters;
using MessagePack;
using System;
using UnityEngine;
using System.Collections;

namespace MessagePackTesting
{
    [Serializable]
    public enum Element
    {
        Virtual = 0,
        Hydrogen = 1,
        Helium = 2,
        Lithium = 3,
        Beryllium = 4,
        Boron = 5,
        Carbon = 6,
        Nitrogen = 7,
        Oxygen = 8,
    }

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

    [MessagePackObject]
    public class Test
    {
        [Key("elements")]
        [MessagePackFormatter(typeof(ElementArray))]
        public Element[] Elements;
    }

    public class MessagePackTestMinimal : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return new WaitForSeconds(.25f);

            var obj1 = new Test
            {
                Elements = new Element[] { Element.Oxygen, Element.Carbon }
            };

            var bytes2 = MessagePackSerializer.Serialize(obj1);
            var obj2 = MessagePackSerializer.Deserialize<Test>(bytes2);

            Debug.Log($"{string.Join(", ", obj1.Elements)}\n");
            Debug.Log($"{string.Join(", ", obj2.Elements)}\n");
        }
    }
}