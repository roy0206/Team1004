using System;

public sealed class SaveStorageException : Exception
{
    public SaveStorageError Error { get; }

    public SaveStorageException(SaveStorageError error, string message)
        : base(message)
    {
        Error = error;
    }

    public SaveStorageException(SaveStorageError error, string message, Exception innerException)
        : base(message, innerException)
    {
        Error = error;
    }
}
