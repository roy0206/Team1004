using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SaveCatalog
{
    private readonly ISaveStorage storage;
    private readonly ISaveCodec codec;

    public SaveCatalog(ISaveStorage saveStorage, ISaveCodec saveCodec = null)
    {
        if (saveStorage == null)
            throw new ArgumentNullException(nameof(saveStorage));

        storage = saveStorage;
        codec = saveCodec ?? SaveEnvelope.CreateDefaultCodec();
    }

    public async Awaitable<IReadOnlyList<SaveSlotInfo>> ListSlotsAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Save name is empty.", nameof(name));

        var keys = await storage.ListKeysAsync();
        var slots = new List<SaveSlotInfo>();

        if (keys == null)
            return slots;

        for (var i = 0; i < keys.Count; i++)
        {
            if (!SaveKey.TryParseSlot(keys[i], name, out var slot))
                continue;

            var envelope = await ReadEnvelopeAsync(keys[i]);
            if (envelope == null)
                continue;

            slots.Add(new SaveSlotInfo(slot, envelope.Version, envelope.SavedAt));
        }

        slots.Sort((left, right) => left.Slot.CompareTo(right.Slot));
        return slots;
    }

    public async Awaitable<THeader> ReadHeaderAsync<THeader>(SaveKey key) where THeader : class
    {
        var envelope = await ReadEnvelopeAsync(key.ToStorageKey());
        if (envelope == null)
            return null;

        try
        {
            return envelope.Data.ToObject<THeader>(SaveEnvelope.Serializer);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[SaveCatalog] '{key}' does not contain a readable {typeof(THeader).Name}. {exception.Message}");

            return null;
        }
    }

    public async Awaitable<bool> ExistsAsync(SaveKey key)
    {
        return await storage.ExistsAsync(key.ToStorageKey());
    }

    public async Awaitable DeleteAsync(SaveKey key)
    {
        var storageKey = key.ToStorageKey();

        await storage.DeleteAsync(storageKey);
        await storage.DeleteAsync(SaveStorageKeys.Backup(storageKey));
        await storage.DeleteAsync(SaveStorageKeys.Corrupt(storageKey));
    }

    private async Awaitable<SaveEnvelope> ReadEnvelopeAsync(string storageKey)
    {
        try
        {
            var bytes = await storage.ReadAsync(storageKey);
            return bytes == null ? null : SaveEnvelope.Decode(codec, bytes);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveCatalog] '{storageKey}' could not be read. {exception.Message}");
            return null;
        }
    }
}
