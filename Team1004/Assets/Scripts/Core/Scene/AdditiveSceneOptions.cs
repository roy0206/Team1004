public readonly struct AdditiveLoadOptions
{
    public static AdditiveLoadOptions Default => new(false, true);

    public bool MakeActive { get; }
    public bool UseTransition { get; }

    public AdditiveLoadOptions(bool makeActive = false, bool useTransition = true)
    {
        MakeActive = makeActive;
        UseTransition = useTransition;
    }
}

public readonly struct AdditiveUnloadOptions
{
    public static AdditiveUnloadOptions Default => new(true);

    public bool UseTransition { get; }

    public AdditiveUnloadOptions(bool useTransition = true)
    {
        UseTransition = useTransition;
    }
}
