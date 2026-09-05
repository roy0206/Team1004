using System;
using UnityEngine;

namespace Game.Settings
{
    public static class RecordService
    {
        private const string SaveName = "Records";
        private const int SaveVersion = 1;

        private static SaveService<RecordData> service;
        private static RecordData fallback;

        public static event Action Changed;

        public static bool IsLoaded => service != null && service.IsLoaded;
        public static int ClearCount => Data.ClearCount;
        public static bool EndingReached => ClearCount > 0;

        public static RecordData Data
        {
            get
            {
                if (IsLoaded)
                    return service.Data;

                fallback ??= new RecordData();
                return fallback;
            }
        }

        public static async Awaitable<SaveLoadResult> LoadAsync()
        {
            service ??= new SaveService<RecordData>(
                new PlayerPrefsSaveStorage(),
                SaveKey.Shared(SaveName),
                SaveVersion);

            var result = await service.LoadAsync();
            RaiseChanged();
            return result;
        }

        public static async Awaitable<SaveWriteResult> SaveAsync()
        {
            if (!IsLoaded)
            {
                Debug.LogWarning("[RecordService] LoadAsync was not called. Records are not saved.");
                return SaveWriteResult.Failed;
            }

            return await service.SaveAsync();
        }

        public static async Awaitable<SaveWriteResult> ReportClearAsync()
        {
            Data.ClearCount++;
            RaiseChanged();
            return await SaveAsync();
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
            service = null;
            fallback = null;
            Changed = null;
        }
    }
}
