using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Game.Config
{
    public static class GameConfigJson
    {
        private sealed class SerializeFieldContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = new List<JsonProperty>();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (field.IsDefined(typeof(NonSerializedAttribute), true))
                        continue;

                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
                        continue;

                    var property = CreateProperty(field, memberSerialization);
                    property.Readable = true;
                    property.Writable = true;
                    properties.Add(property);
                }

                return properties;
            }
        }

        public static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new SerializeFieldContractResolver(),
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        public static string ToJson(GameConfigValues values)
        {
            return JsonConvert.SerializeObject(values, Settings);
        }

        public static void Populate(string json, GameConfigValues target)
        {
            JsonConvert.PopulateObject(json, target, Settings);
        }

        public static GameConfigValues Clone(GameConfigValues values)
        {
            return JsonConvert.DeserializeObject<GameConfigValues>(ToJson(values), Settings) ?? new GameConfigValues();
        }
    }
}
