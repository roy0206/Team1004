using System;

public readonly struct SaveSlotInfo
{
    public int Slot { get; }
    public int Version { get; }
    public DateTimeOffset SavedAt { get; }

    public SaveSlotInfo(int slot, int version, DateTimeOffset savedAt)
    {
        Slot = slot;
        Version = version;
        SavedAt = savedAt;
    }
}
