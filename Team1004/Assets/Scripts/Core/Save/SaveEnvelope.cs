using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal sealed class SaveEnvelope
{
    private const string VersionField = "version";
    private const string SavedAtField = "savedAt";
    private const string DataField = "data";

    public static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        DateParseHandling = DateParseHandling.DateTimeOffset,
        Converters = { new StringEnumConverter() }
    });

    public int Version { get; private set; }
    public DateTimeOffset SavedAt { get; private set; }
    public JObject Data { get; private set; }

    public static ISaveCodec CreateDefaultCodec()
    {
        return new ChecksumSaveCodec(new Utf8SaveCodec());
    }

    public static SaveEnvelope Decode(ISaveCodec codec, byte[] bytes)
    {
        return Parse(codec.Decode(bytes));
    }

    public static SaveEnvelope Parse(string json)
    {
        var root = JObject.Parse(json);

        if (root[VersionField] is JValue version && root[DataField] is JObject data)
        {
            return new SaveEnvelope
            {
                Version = version.Value<int>(),
                SavedAt = ReadSavedAt(root),
                Data = data
            };
        }

        return new SaveEnvelope
        {
            Version = 0,
            SavedAt = default,
            Data = root
        };
    }

    public static string Write(int version, JObject data)
    {
        var root = new JObject
        {
            [VersionField] = version,
            [SavedAtField] = DateTimeOffset.UtcNow,
            [DataField] = data
        };

        return root.ToString(Formatting.Indented);
    }

    private static DateTimeOffset ReadSavedAt(JObject root)
    {
        var savedAt = root[SavedAtField];
        if (savedAt == null || savedAt.Type == JTokenType.Null)
            return default;

        try
        {
            return (DateTimeOffset)savedAt;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            return default;
        }
    }
}
