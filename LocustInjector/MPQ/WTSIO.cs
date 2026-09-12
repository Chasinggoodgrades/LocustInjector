using System.Text;

public static class WtsIO
{
    private static readonly UTF8Encoding WtsEncoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Decode(byte[] raw)
    {
        string content = WtsEncoding.GetString(raw);

        // THIS SHITY HEX CODE :EF BB BF: FIX
        if (content.Length > 0 && content[0] == '\uFEFF')
            content = content[1..];

        return content;
    }

    public static byte[] Encode(string content) => WtsEncoding.GetBytes(content);

    public static string Read(string path) => Decode(File.ReadAllBytes(path));

    public static void Write(string path, string content) => File.WriteAllBytes(path, Encode(content));
}