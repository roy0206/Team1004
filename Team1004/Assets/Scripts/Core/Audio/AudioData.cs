using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class AudioManifest
{
    [JsonProperty("settings")]
    public AudioRuntimeSettings Settings { get; set; } = new();

    [JsonProperty("sounds")]
    public IReadOnlyList<AudioEntryData> Sounds { get; set; } = Array.Empty<AudioEntryData>();
}

public sealed class AudioEntryData
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("loadKey")]
    public string LoadKey { get; set; }
}

public sealed class AudioRuntimeSettings
{
    [JsonProperty("initialPoolSize")]
    public int InitialPoolSize { get; set; } = 12;

    [JsonProperty("maxSfxChannels")]
    public int MaxSfxChannels { get; set; } = 32;

    [JsonProperty("spatialBlend")]
    public float SpatialBlend { get; set; } = 1f;

    [JsonProperty("minDistance")]
    public float MinDistance { get; set; } = 1f;

    [JsonProperty("maxDistance")]
    public float MaxDistance { get; set; } = 15f;

    [JsonProperty("masterVolume")]
    public float MasterVolume { get; set; } = 1f;

    [JsonProperty("sfxVolume")]
    public float SfxVolume { get; set; } = 1f;

    [JsonProperty("bgmVolume")]
    public float BgmVolume { get; set; } = 1f;

    [JsonProperty("masterMuted")]
    public bool MasterMuted { get; set; }

    [JsonProperty("sfxMuted")]
    public bool SfxMuted { get; set; }

    [JsonProperty("bgmMuted")]
    public bool BgmMuted { get; set; }

    public void Validate()
    {
        if (MaxSfxChannels < 1)
            throw new ArgumentException("Max SFX channels must be 1 or greater.", nameof(MaxSfxChannels));

        if (InitialPoolSize < 1 || InitialPoolSize > MaxSfxChannels)
            throw new ArgumentException("Initial pool size must be between 1 and max SFX channels.", nameof(InitialPoolSize));

        if (SpatialBlend < 0f || SpatialBlend > 1f)
            throw new ArgumentException("Spatial blend must be between 0 and 1.", nameof(SpatialBlend));

        if (MinDistance <= 0f)
            throw new ArgumentException("Min distance must be greater than zero.", nameof(MinDistance));

        if (MaxDistance < MinDistance)
            throw new ArgumentException("Max distance must be greater than or equal to min distance.", nameof(MaxDistance));

        ValidateVolume(MasterVolume, nameof(MasterVolume));
        ValidateVolume(SfxVolume, nameof(SfxVolume));
        ValidateVolume(BgmVolume, nameof(BgmVolume));
    }

    private static void ValidateVolume(float value, string name)
    {
        if (value < 0f || value > 1f)
            throw new ArgumentException("Volume must be between 0 and 1.", name);
    }
}
