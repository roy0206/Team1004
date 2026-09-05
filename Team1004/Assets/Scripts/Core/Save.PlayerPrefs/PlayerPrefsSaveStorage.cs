using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerPrefsSaveStorage : ISaveStorage
{
    private const string DefaultPrefix = "Save";
    private const string IndexSuffix = "__index";
    private const char IndexSeparator = '\n';

    private readonly string prefix;
    private readonly string indexKey;
    private HashSet<string> index;

    public PlayerPrefsSaveStorage()
        : this(DefaultPrefix)
    {
    }

    public PlayerPrefsSaveStorage(string keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("Save key prefix is empty.", nameof(keyPrefix));

        prefix = keyPrefix;
        indexKey = Combine(IndexSuffix);
    }

    public Awaitable<byte[]> ReadAsync(string key)
    {
        var storedKey = Combine(Validate(key));
        var source = new AwaitableCompletionSource<byte[]>();

        if (!PlayerPrefs.HasKey(storedKey))
        {
            source.SetResult(null);
            return source.Awaitable;
        }

        try
        {
            source.SetResult(Convert.FromBase64String(PlayerPrefs.GetString(storedKey)));
        }
        catch (FormatException exception)
        {
            source.SetException(new SaveStorageException(
                SaveStorageError.Failed,
                $"Save '{key}' is not valid Base64. {exception.Message}",
                exception));
        }

        return source.Awaitable;
    }

    public Awaitable WriteAsync(string key, byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var validKey = Validate(key);
        var source = new AwaitableCompletionSource();

        try
        {
            PlayerPrefs.SetString(Combine(validKey), Convert.ToBase64String(data));
            AddToIndex(validKey);
            PlayerPrefs.Save();
            source.SetResult();
        }
        catch (PlayerPrefsException exception)
        {
            source.SetException(new SaveStorageException(
                SaveStorageError.StorageFull,
                $"Save '{key}' exceeds the PlayerPrefs size limit. {exception.Message}",
                exception));
        }
        catch (Exception exception)
        {
            source.SetException(new SaveStorageException(
                SaveStorageError.Failed,
                $"Save '{key}' could not be written. {exception.Message}",
                exception));
        }

        return source.Awaitable;
    }

    public Awaitable DeleteAsync(string key)
    {
        var validKey = Validate(key);
        var source = new AwaitableCompletionSource();

        try
        {
            PlayerPrefs.DeleteKey(Combine(validKey));
            RemoveFromIndex(validKey);
            PlayerPrefs.Save();
            source.SetResult();
        }
        catch (Exception exception)
        {
            source.SetException(new SaveStorageException(
                SaveStorageError.Failed,
                $"Save '{key}' could not be deleted. {exception.Message}",
                exception));
        }

        return source.Awaitable;
    }

    public Awaitable<bool> ExistsAsync(string key)
    {
        var source = new AwaitableCompletionSource<bool>();
        source.SetResult(PlayerPrefs.HasKey(Combine(Validate(key))));

        return source.Awaitable;
    }

    public Awaitable<IReadOnlyList<string>> ListKeysAsync()
    {
        var source = new AwaitableCompletionSource<IReadOnlyList<string>>();
        source.SetResult(ReadIndex());

        return source.Awaitable;
    }

    private IReadOnlyList<string> ReadIndex()
    {
        return new List<string>(GetIndex());
    }

    private HashSet<string> GetIndex()
    {
        if (index != null)
            return index;

        index = new HashSet<string>(StringComparer.Ordinal);

        var stored = PlayerPrefs.GetString(indexKey, string.Empty);
        if (stored.Length == 0)
            return index;

        var entries = stored.Split(IndexSeparator);
        for (var i = 0; i < entries.Length; i++)
        {
            if (entries[i].Length > 0 && PlayerPrefs.HasKey(Combine(entries[i])))
                index.Add(entries[i]);
        }

        return index;
    }

    private void AddToIndex(string key)
    {
        if (GetIndex().Add(key))
            WriteIndex();
    }

    private void RemoveFromIndex(string key)
    {
        if (GetIndex().Remove(key))
            WriteIndex();
    }

    private void WriteIndex()
    {
        PlayerPrefs.SetString(indexKey, string.Join(IndexSeparator.ToString(), index));
    }

    private string Combine(string key)
    {
        return string.Concat(prefix, ".", key);
    }

    private static string Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Save key is empty.", nameof(key));

        if (key.IndexOf(IndexSeparator) >= 0)
            throw new ArgumentException($"Save key must not contain a line break: '{key}'.", nameof(key));

        if (string.Equals(key, IndexSuffix, StringComparison.Ordinal))
            throw new ArgumentException($"Save key is reserved: '{key}'.", nameof(key));

        return key;
    }
}
