using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nanover.Core.Serialization
{
    /// <summary>
    /// A <see cref="JsonReader" /> for use with json.NET that can convert an object from a
    /// JSON-like representation, consisting of <see cref="Dictionary{TKey,TValue}" />,
    /// <see cref="List{Object}" />, <see cref="string" />, <see cref="float" />,
    /// <see cref="string" /> and <see cref="bool" />.
    /// </summary>
    public class CSharpObjectReader : JsonReader
    {
        private Stack<object> objects = new Stack<object>();

        private IEnumerator<(JsonToken, object)> iterator;

        public CSharpObjectReader(object obj)
        {
            iterator = Iterate(obj).GetEnumerator();
        }

        private static IEnumerable<(JsonToken, object)> Iterate(object obj)
        {
            switch (obj)
            {
                case IReadOnlyDictionary<object, object> dictAnnoying:
                    yield return (JsonToken.StartObject, null);
                    foreach (var (key, value) in dictAnnoying)
                    {
                        yield return (JsonToken.PropertyName, key);
                        foreach (var v in Iterate(value))
                            yield return v;
                    }
                    yield return (JsonToken.EndObject, null);
                    break;
                case IReadOnlyDictionary<string, object> dict:
                    yield return (JsonToken.StartObject, null);
                    foreach (var (key, value) in dict)
                    {
                        yield return (JsonToken.PropertyName, key);
                        foreach (var v in Iterate(value))
                            yield return v;
                    }
                    yield return (JsonToken.EndObject, null);
                    break;
                case IReadOnlyList<object> list:
                    yield return (JsonToken.StartArray, null);
                    foreach(var item in list)
                    foreach (var v in Iterate(item))
                        yield return v;
                    yield return (JsonToken.EndArray, null);
                    break;
                case string str:
                    yield return (JsonToken.String, str);
                    break;
                case bool bol:
                    yield return (JsonToken.Boolean, bol);
                    break;
                case int nt:
                    yield return (JsonToken.Integer, nt);
                    break;
                case UInt64 nt:
                    yield return (JsonToken.Integer, nt);
                    break;
                case float flt:
                    yield return (JsonToken.Float, flt);
                    break;
                case double dbl:
                    yield return (JsonToken.Float, dbl);
                    break;
                default:
                    throw new ArgumentException($"Cannot parse {obj} ({obj.GetType()})");
            }
        }

        public override bool Read()
        {
            if (!iterator.MoveNext())
                return false;
            var (jsonToken, value) = iterator.Current;
            SetToken(jsonToken, value);
            return true;
        }
    }
}
