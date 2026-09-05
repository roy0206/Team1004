using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class SaveService<TData> where TData : class, new()
{
    private enum ReadStatus
    {
        Missing,
        Loaded,
        Unsupported,
        Corrupted,
        Unavailable
    }

    private readonly struct ReadOutcome
    {
        public ReadStatus Status { get; }
        public TData Value { get; }
        public bool WasMigrated { get; }

        public ReadOutcome(ReadStatus status, TData value, bool wasMigrated)
        {
            Status = status;
            Value = value;
            WasMigrated = wasMigrated;
        }
    }

    private readonly ISaveStorage storage;
    private readonly ISaveCodec codec;
    private readonly IReadOnlyList<ISaveMigration> migrations;
    private readonly string storageKey;
    private readonly string backupKey;
    private readonly string corruptKey;
    private readonly bool keepBackup;

    private TData data;
    private bool isSaving;
    private bool primaryIsTrusted;

    public SaveService(
        ISaveStorage saveStorage,
        SaveKey saveKey,
        int currentVersion,
        ISaveCodec saveCodec = null,
        IEnumerable<ISaveMigration> saveMigrations = null,
        bool keepBackupCopy = true)
    {
        if (saveStorage == null)
            throw new ArgumentNullException(nameof(saveStorage));

        if (currentVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(currentVersion), "Save version must be 1 or greater.");

        storage = saveStorage;
        codec = saveCodec ?? SaveEnvelope.CreateDefaultCodec();
        migrations = ValidateMigrations(saveMigrations, currentVersion);
        keepBackup = keepBackupCopy;

        Key = saveKey;
        Version = currentVersion;
        storageKey = saveKey.ToStorageKey();
        backupKey = SaveStorageKeys.Backup(storageKey);
        corruptKey = SaveStorageKeys.Corrupt(storageKey);
    }

    public event Action SaveStarted;
    public event Action<SaveWriteResult> SaveCompleted;

    public SaveKey Key { get; }
    public int Version { get; }
    public bool IsLoaded { get; private set; }
    public bool IsSaving => isSaving;
    public SaveLoadResult LastLoadResult { get; private set; }

    public TData Data
    {
        get
        {
            EnsureLoaded();
            return data;
        }
    }

    public bool TryGetData(out TData value)
    {
        value = IsLoaded ? data : null;
        return IsLoaded;
    }

    public async Awaitable<SaveLoadResult> LoadAsync()
    {
        var primary = await ReadAsync(storageKey);

        if (primary.Status == ReadStatus.Loaded)
        {
            return Complete(
                primary.Value,
                primary.WasMigrated ? SaveLoadResult.Migrated : SaveLoadResult.Loaded);
        }

        if (primary.Status == ReadStatus.Unavailable)
            return Complete(null, SaveLoadResult.Unavailable);

        if (primary.Status == ReadStatus.Missing)
        {
            var orphan = await ReadAsync(backupKey);

            return orphan.Status == ReadStatus.Loaded
                ? Complete(orphan.Value, SaveLoadResult.Recovered)
                : Complete(null, SaveLoadResult.Created);
        }

        await PreserveUnreadableAsync();

        var backup = await ReadAsync(backupKey);
        if (backup.Status == ReadStatus.Loaded)
            return Complete(backup.Value, SaveLoadResult.Recovered);

        return Complete(
            null,
            primary.Status == ReadStatus.Unsupported ? SaveLoadResult.Unsupported : SaveLoadResult.Corrupted);
    }

    public async Awaitable<SaveWriteResult> SaveAsync()
    {
        EnsureLoaded();

        if (LastLoadResult == SaveLoadResult.Unavailable)
        {
            Debug.LogError(
                $"[SaveService] '{storageKey}' was not readable at load time. Saving is refused until LoadAsync succeeds or ResetAsync is called.");
            return SaveWriteResult.Failed;
        }

        while (isSaving)
            await Awaitable.NextFrameAsync();

        var result = SaveWriteResult.Success;

        try
        {
            isSaving = true;
            Raise(SaveStarted);

            var json = SaveEnvelope.Write(Version, JObject.FromObject(data, SaveEnvelope.Serializer));
            var bytes = codec.Encode(json);

            if (keepBackup && primaryIsTrusted)
                await BackupAsync();

            await storage.WriteAsync(storageKey, bytes);
            primaryIsTrusted = true;
        }
        catch (SaveStorageException exception)
        {
            result = Map(exception.Error);
            Debug.LogError($"[SaveService] '{storageKey}' could not be written. {exception.Message}");
        }
        catch (Exception exception)
        {
            result = SaveWriteResult.Failed;
            Debug.LogError($"[SaveService] '{storageKey}' could not be written. {exception.Message}");
        }
        finally
        {
            isSaving = false;
        }

        Raise(SaveCompleted, result);
        return result;
    }

    public Awaitable<SaveWriteResult> ResetAsync()
    {
        data = new TData();
        IsLoaded = true;
        LastLoadResult = SaveLoadResult.Created;

        return SaveAsync();
    }

    private async Awaitable<ReadOutcome> ReadAsync(string targetKey)
    {
        byte[] bytes;

        try
        {
            bytes = await storage.ReadAsync(targetKey);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveService] '{targetKey}' could not be read. {exception.Message}");
            return new ReadOutcome(ReadStatus.Unavailable, null, false);
        }

        if (bytes == null)
            return new ReadOutcome(ReadStatus.Missing, null, false);

        try
        {
            var envelope = SaveEnvelope.Decode(codec, bytes);

            if (envelope.Version > Version)
            {
                Debug.LogError(
                    $"[SaveService] '{targetKey}' was written by save version {envelope.Version}, " +
                    $"which is newer than the supported version {Version}.");

                return new ReadOutcome(ReadStatus.Unsupported, null, false);
            }

            var wasMigrated = Migrate(envelope);
            var value = envelope.Data.ToObject<TData>(SaveEnvelope.Serializer);

            return new ReadOutcome(ReadStatus.Loaded, value ?? new TData(), wasMigrated);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveService] '{targetKey}' could not be interpreted. {exception.Message}");
            return new ReadOutcome(ReadStatus.Corrupted, null, false);
        }
    }

    private bool Migrate(SaveEnvelope envelope)
    {
        var applied = false;

        for (var i = 0; i < migrations.Count; i++)
        {
            var migration = migrations[i];
            if (migration.TargetVersion <= envelope.Version)
                continue;

            migration.Apply(envelope.Data);
            applied = true;
        }

        return applied;
    }

    private async Awaitable PreserveUnreadableAsync()
    {
        try
        {
            var bytes = await storage.ReadAsync(storageKey);
            if (bytes != null)
                await storage.WriteAsync(corruptKey, bytes);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[SaveService] Unreadable save '{storageKey}' could not be preserved. {exception.Message}");
        }
    }

    private async Awaitable BackupAsync()
    {
        try
        {
            var existing = await storage.ReadAsync(storageKey);
            if (existing != null)
                await storage.WriteAsync(backupKey, existing);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveService] Backup of '{storageKey}' failed. {exception.Message}");
        }
    }

    private static void Raise(Action handler)
    {
        if (handler == null)
            return;

        foreach (var target in handler.GetInvocationList())
        {
            try
            {
                ((Action)target)();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void Raise(Action<SaveWriteResult> handler, SaveWriteResult result)
    {
        if (handler == null)
            return;

        foreach (var target in handler.GetInvocationList())
        {
            try
            {
                ((Action<SaveWriteResult>)target)(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private SaveLoadResult Complete(TData value, SaveLoadResult result)
    {
        data = value ?? new TData();
        IsLoaded = true;
        LastLoadResult = result;
        primaryIsTrusted = result == SaveLoadResult.Loaded || result == SaveLoadResult.Migrated;

        return result;
    }

    private void EnsureLoaded()
    {
        if (!IsLoaded)
            throw new InvalidOperationException($"Save '{storageKey}' is not loaded. Call LoadAsync first.");
    }

    private static SaveWriteResult Map(SaveStorageError error)
    {
        return error switch
        {
            SaveStorageError.StorageFull => SaveWriteResult.StorageFull,
            SaveStorageError.AccessDenied => SaveWriteResult.AccessDenied,
            _ => SaveWriteResult.Failed
        };
    }

    private static IReadOnlyList<ISaveMigration> ValidateMigrations(
        IEnumerable<ISaveMigration> source,
        int currentVersion)
    {
        if (source == null)
            return Array.Empty<ISaveMigration>();

        var ordered = new List<ISaveMigration>();
        var targets = new HashSet<int>();

        foreach (var migration in source)
        {
            if (migration == null)
                throw new ArgumentException("Save migrations contain a null entry.", nameof(source));

            if (migration.TargetVersion < 1)
                throw new ArgumentException(
                    $"Save migration '{migration.GetType().Name}' targets version {migration.TargetVersion}. " +
                    "Target versions start at 1.",
                    nameof(source));

            if (migration.TargetVersion > currentVersion)
                throw new ArgumentException(
                    $"Save migration '{migration.GetType().Name}' targets version {migration.TargetVersion}, " +
                    $"which is newer than the current version {currentVersion}.",
                    nameof(source));

            if (!targets.Add(migration.TargetVersion))
                throw new ArgumentException(
                    $"Two save migrations target version {migration.TargetVersion}.",
                    nameof(source));

            ordered.Add(migration);
        }

        ordered.Sort((left, right) => left.TargetVersion.CompareTo(right.TargetVersion));
        return ordered;
    }
}
