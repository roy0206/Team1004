using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class FileSaveStorage : ISaveStorage
{
    private const string Extension = ".sav";
    private const string TemporaryExtension = ".tmp";
    private const int ErrorHandleDiskFull = 0x27;
    private const int ErrorDiskFull = 0x70;

    private static readonly char[] InvalidKeyChars = Path.GetInvalidFileNameChars();

    private readonly object writeLock = new();
    private readonly string directory;

    public FileSaveStorage()
        : this(Path.Combine(Application.persistentDataPath, "Saves"))
    {
    }

    public FileSaveStorage(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Save directory is empty.", nameof(rootDirectory));

        directory = rootDirectory;
    }

    public Awaitable<byte[]> ReadAsync(string key)
    {
        var path = GetPath(key);

        return RunAsync(key, () =>
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        });
    }

    public async Awaitable WriteAsync(string key, byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var path = GetPath(key);

        await RunAsync(key, () =>
        {
            WriteAtomic(path, data);
            return true;
        });
    }

    public async Awaitable DeleteAsync(string key)
    {
        var path = GetPath(key);

        await RunAsync(key, () =>
        {
            lock (writeLock)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }

            return true;
        });
    }

    public Awaitable<bool> ExistsAsync(string key)
    {
        var path = GetPath(key);
        return RunAsync(key, () => File.Exists(path));
    }

    public Awaitable<IReadOnlyList<string>> ListKeysAsync()
    {
        return RunAsync<IReadOnlyList<string>>(directory, () =>
        {
            var keys = new List<string>();
            if (!Directory.Exists(directory))
                return keys;

            var files = Directory.GetFiles(directory, "*" + Extension);
            for (var i = 0; i < files.Length; i++)
                keys.Add(Path.GetFileNameWithoutExtension(files[i]));

            return keys;
        });
    }

    private static async Awaitable<T> RunAsync<T>(string key, Func<T> work)
    {
        T result = default;
        SaveStorageException failure = null;

        await Awaitable.BackgroundThreadAsync();

        try
        {
            result = work();
        }
        catch (Exception exception)
        {
            failure = Translate(exception, key);
        }

        await Awaitable.MainThreadAsync();

        if (failure != null)
            throw failure;

        return result;
    }

    private void WriteAtomic(string path, byte[] data)
    {
        lock (writeLock)
        {
            Directory.CreateDirectory(directory);

            var temporaryPath = path + TemporaryExtension;

            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }

            if (!File.Exists(path))
            {
                File.Move(temporaryPath, path);
                return;
            }

            try
            {
                File.Replace(temporaryPath, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(temporaryPath, path);
            }
        }
    }

    private string GetPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Save key is empty.", nameof(key));

        if (key.IndexOfAny(InvalidKeyChars) >= 0)
            throw new ArgumentException($"Save key is not a valid file name: '{key}'.", nameof(key));

        return Path.Combine(directory, key + Extension);
    }

    private static SaveStorageException Translate(Exception exception, string key)
    {
        if (exception is UnauthorizedAccessException)
        {
            return new SaveStorageException(
                SaveStorageError.AccessDenied,
                $"Access to save '{key}' was denied. {exception.Message}",
                exception);
        }

        if (exception is IOException ioException && IsDiskFull(ioException))
        {
            return new SaveStorageException(
                SaveStorageError.StorageFull,
                $"Save '{key}' could not be written because the storage device is full.",
                exception);
        }

        return new SaveStorageException(
            SaveStorageError.Failed,
            $"Save '{key}' could not be accessed. {exception.Message}",
            exception);
    }

    private static bool IsDiskFull(IOException exception)
    {
        var code = exception.HResult & 0xFFFF;
        return code == ErrorHandleDiskFull || code == ErrorDiskFull;
    }
}
