using Newtonsoft.Json;

namespace Game.Settings
{
    public sealed class RecordData
    {
        [JsonProperty("clearCount")]
        public int ClearCount { get; set; }
    }
}
