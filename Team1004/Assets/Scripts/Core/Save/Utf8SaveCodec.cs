using System;
using System.Text;

public sealed class Utf8SaveCodec : ISaveCodec
{
    private static readonly UTF8Encoding Encoding = new(false, true);

    public byte[] Encode(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return Encoding.GetBytes(text);
    }

    public string Decode(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            return Encoding.GetString(data);
        }
        catch (DecoderFallbackException exception)
        {
            throw new SaveCodecException("Save data is not valid UTF-8.", exception);
        }
    }
}
