using System;

public sealed class PoolSettings
{
    public int MaxPooledPerKey { get; set; }
    public bool UseUnscaledTime { get; set; }

    public void Validate()
    {
        if (MaxPooledPerKey < 0)
            throw new ArgumentException("Max pooled per key must be zero or greater.", nameof(MaxPooledPerKey));
    }
}
