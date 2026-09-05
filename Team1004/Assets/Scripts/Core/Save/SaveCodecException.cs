using System;

public sealed class SaveCodecException : Exception
{
    public SaveCodecException(string message)
        : base(message)
    {
    }

    public SaveCodecException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
