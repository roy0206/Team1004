public interface ISaveCodec
{
    byte[] Encode(string text);
    string Decode(byte[] data);
}
