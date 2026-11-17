using System.IO.Compression;
using System.Security.Cryptography;

namespace ConvictionCore.Infrastructure.Ibkr.Flex;

internal static class FlexPayloadCodec
{
    public const string Encoding = "gzip";

    public static string Sha256Hex(ReadOnlySpan<byte> uncompressed) =>
        Convert.ToHexStringLower(SHA256.HashData(uncompressed));

    public static byte[] Compress(byte[] uncompressed)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(uncompressed);
        }

        return output.ToArray();
    }

    public static byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
