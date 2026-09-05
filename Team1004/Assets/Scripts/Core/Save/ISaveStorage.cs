using System.Collections.Generic;
using UnityEngine;

public interface ISaveStorage
{
    Awaitable<byte[]> ReadAsync(string key);
    Awaitable WriteAsync(string key, byte[] data);
    Awaitable DeleteAsync(string key);
    Awaitable<bool> ExistsAsync(string key);
    Awaitable<IReadOnlyList<string>> ListKeysAsync();
}
