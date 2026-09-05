using System;
using System.IO;
using UnityEngine;

namespace Game.Config
{
    public static class GameConfig
    {
        private const string OverrideFileName = "config_override.json";

        private static GameConfigValues baseValues;
        private static GameConfigValues current;
        private static bool missingLoadWarned;

        public static event Action Changed;

        public static bool IsLoaded => baseValues != null;
        public static string OverridePath => Path.Combine(Application.persistentDataPath, OverrideFileName);
        public static bool HasOverride => File.Exists(OverridePath);

        public static GameConfigValues Current
        {
            get
            {
                if (current != null)
                    return current;

                if (!missingLoadWarned)
                {
                    missingLoadWarned = true;
                    Debug.LogWarning("[GameConfig] Load was not called. Using default values.");
                }

                current = new GameConfigValues();
                return current;
            }
        }

        public static void Load(GameConfigAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            baseValues = asset.Values.Clone();
            var values = baseValues.Clone();

            if (HasOverride)
            {
                try
                {
                    GameConfigJson.Populate(File.ReadAllText(OverridePath), values);

                    if (!values.Validate(out var error))
                    {
                        Debug.LogWarning($"[GameConfig] Override file is invalid and was ignored. {error}");
                        values = baseValues.Clone();
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[GameConfig] Override file could not be read and was ignored. {exception.Message}");
                    values = baseValues.Clone();
                }
            }

            current = values;
            RaiseChanged();
        }

        public static string ToJson()
        {
            return GameConfigJson.ToJson(Current);
        }

        public static bool TryApplyJson(string json, out string error)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON is empty.";
                return false;
            }

            var candidate = Current.Clone();

            try
            {
                GameConfigJson.Populate(json, candidate);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (!candidate.Validate(out error))
                return false;

            current = candidate;
            RaiseChanged();
            return true;
        }

        public static bool SaveOverride(out string error)
        {
            try
            {
                var directory = Path.GetDirectoryName(OverridePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(OverridePath, ToJson());
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool ResetOverride(out string error)
        {
            try
            {
                if (HasOverride)
                    File.Delete(OverridePath);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            current = (baseValues ?? new GameConfigValues()).Clone();
            RaiseChanged();
            error = null;
            return true;
        }

        private static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            baseValues = null;
            current = null;
            missingLoadWarned = false;
            Changed = null;
        }
    }
}
