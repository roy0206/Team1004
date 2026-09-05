using Newtonsoft.Json;

namespace Game.Settings
{
    public sealed class SettingsData
    {
        [JsonProperty("masterVolume")]
        public float MasterVolume { get; set; } = 1f;

        [JsonProperty("bgmVolume")]
        public float BgmVolume { get; set; } = 1f;

        [JsonProperty("sfxVolume")]
        public float SfxVolume { get; set; } = 1f;
    }
}
