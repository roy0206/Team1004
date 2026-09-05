using UnityEngine;

namespace Game.Settings
{
    public static class SettingsService
    {
        private const string SaveName = "Settings";
        private const int SaveVersion = 1;

        private static SaveService<SettingsData> service;
        private static SettingsData fallback;

        public static bool IsLoaded => service != null && service.IsLoaded;

        public static SettingsData Data
        {
            get
            {
                if (IsLoaded)
                    return service.Data;

                fallback ??= new SettingsData();
                return fallback;
            }
        }

        public static async Awaitable<SaveLoadResult> LoadAsync()
        {
            service ??= new SaveService<SettingsData>(
                new PlayerPrefsSaveStorage(),
                SaveKey.Shared(SaveName),
                SaveVersion);

            var result = await service.LoadAsync();
            Clamp(service.Data);
            return result;
        }

        public static async Awaitable<SaveWriteResult> SaveAsync()
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("[SettingsService] LoadAsync was not called. Settings are not saved.");
                return SaveWriteResult.Failed;
            }

            return await service.SaveAsync();
        }

        public static void Apply()
        {
            if (!AudioManager.TryGetInstance(out var audio))
                return;

            var data = Data;
            audio.MasterVolume = Mathf.Clamp01(data.MasterVolume);
            audio.BgmVolume = Mathf.Clamp01(data.BgmVolume);
            audio.SfxVolume = Mathf.Clamp01(data.SfxVolume);
        }

        public static void SetMasterVolume(float value)
        {
            Data.MasterVolume = Mathf.Clamp01(value);
            Apply();
        }

        public static void SetBgmVolume(float value)
        {
            Data.BgmVolume = Mathf.Clamp01(value);
            Apply();
        }

        public static void SetSfxVolume(float value)
        {
            Data.SfxVolume = Mathf.Clamp01(value);
            Apply();
        }

        private static void Clamp(SettingsData data)
        {
            data.MasterVolume = Mathf.Clamp01(data.MasterVolume);
            data.BgmVolume = Mathf.Clamp01(data.BgmVolume);
            data.SfxVolume = Mathf.Clamp01(data.SfxVolume);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            service = null;
            fallback = null;
        }
    }
}
