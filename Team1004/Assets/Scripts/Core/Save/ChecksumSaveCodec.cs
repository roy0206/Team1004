using System;

public sealed class ChecksumSaveCodec : ISaveCodec
{
    private const int ChecksumLength = 8;
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    private readonly ISaveCodec inner;

    public ChecksumSaveCodec(ISaveCodec innerCodec)
    {
        if (innerCodec == null)
            throw new ArgumentNullException(nameof(innerCodec));

        inner = innerCodec;
    }

    public byte[] Encode(string text)
    {
        var payload = inner.Encode(text);
        var result = new byte[ChecksumLength + payload.Length];

        WriteChecksum(Compute(payload, 0, payload.Length), result);
        Buffer.BlockCopy(payload, 0, result, ChecksumLength, payload.Length);

        return result;
    }

    public string Decode(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length < ChecksumLength)
            throw new SaveCodecException("Save data is shorter than its checksum header.");

        var expected = ReadChecksum(data);
        var actual = Compute(data, ChecksumLength, data.Length - ChecksumLength);

        if (expected != actual)
            throw new SaveCodecException("Save data checksum does not match. The data was damaged or modified.");

        var payload = new byte[data.Length - ChecksumLength];
        Buffer.BlockCopy(data, ChecksumLength, payload, 0, payload.Length);

        return inner.Decode(payload);
    }

    private static ulong Compute(byte[] data, int offset, int count)
    {
        var hash = OffsetBasis;

        for (var i = offset; i < offset + count; i++)
        {
            hash ^= data[i];
            hash *= Prime;
        }

        return hash;
    }

    private static void WriteChecksum(ulong checksum, byte[] target)
    {
        for (var i = 0; i < ChecksumLength; i++)
            target[i] = (byte)(checksum >> (i * 8));
    }

    private static ulong ReadChecksum(byte[] source)
    {
        var checksum = 0UL;

        for (var i = 0; i < ChecksumLength; i++)
            checksum |= (ulong)source[i] << (i * 8);

        return checksum;
    }
}
