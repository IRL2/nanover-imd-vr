using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nanover.Core.Serialization
{
    /// <summary>
    /// <see cref="JsonConverter{T}" /> for serializing a <see cref="Vector3" /> as a
    /// list of three floats.
    /// </summary>
    public class Vector3Converter : JsonConverter
    {
        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                        object existingValue,
                                        JsonSerializer serializer)
        {
            var obj = JToken.Load(reader);
            if (obj.Type == JTokenType.Array)
            {
                var arr = (JArray) obj;
                if (arr.Count == 3 && arr.All(token => token.Type == JTokenType.Float || token.Type == JTokenType.Integer))
                {
                    return new Vector3(arr[0].Value<float>(), arr[1].Value<float>(), arr[2].Value<float>());
                }
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var vector = (Vector3) value;
            writer.WriteStartArray();
            writer.WriteValue(vector.x);
            writer.WriteValue(vector.y);
            writer.WriteValue(vector.z);
            writer.WriteEndArray();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }
    }
}