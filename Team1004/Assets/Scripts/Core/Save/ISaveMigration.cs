using Newtonsoft.Json.Linq;

public interface ISaveMigration
{
    int TargetVersion { get; }
    void Apply(JObject data);
}
