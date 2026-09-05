using System;
using System.Globalization;

public readonly struct SaveKey : IEquatable<SaveKey>
{
    private const int SharedSlot = -1;
    private const char SlotSeparator = '.';

    public string Name { get; }
    public int Slot { get; }

    private SaveKey(string name, int slot)
    {
        Name = name;
        Slot = slot;
    }

    public bool IsSlotted => Slot >= 0;

    public static SaveKey Shared(string name)
    {
        return new SaveKey(ValidateName(name), SharedSlot);
    }

    public static SaveKey Slotted(string name, int slot)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot), "Save slot index must not be negative.");

        return new SaveKey(ValidateName(name), slot);
    }

    public string ToStorageKey()
    {
        if (Name == null)
            throw new InvalidOperationException("Save key was never initialized.");

        return IsSlotted
            ? string.Concat(Name, SlotSeparator.ToString(), Slot.ToString(CultureInfo.InvariantCulture))
            : Name;
    }

    public static bool TryParseSlot(string storageKey, string name, out int slot)
    {
        slot = SharedSlot;

        if (string.IsNullOrEmpty(storageKey) || string.IsNullOrEmpty(name))
            return false;

        if (storageKey.Length <= name.Length + 1)
            return false;

        if (!storageKey.StartsWith(name, StringComparison.Ordinal))
            return false;

        if (storageKey[name.Length] != SlotSeparator)
            return false;

        return int.TryParse(
            storageKey.Substring(name.Length + 1),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out slot);
    }

    public bool Equals(SaveKey other)
    {
        return Slot == other.Slot && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is SaveKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Slot);
    }

    public override string ToString()
    {
        return Name == null ? "<uninitialized>" : ToStorageKey();
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Save name is empty.", nameof(name));

        if (name.IndexOf(SlotSeparator) >= 0)
            throw new ArgumentException($"Save name must not contain '{SlotSeparator}': '{name}'.", nameof(name));

        return name;
    }
}
