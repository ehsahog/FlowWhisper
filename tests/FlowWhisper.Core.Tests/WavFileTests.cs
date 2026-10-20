using System.Text;
using FlowWhisper.Core.Audio;

namespace FlowWhisper.Core.Tests;

public class WavFileTests
{
    private static byte[] Pcm(params short[] samples) => samples.SelectMany(BitConverter.GetBytes).ToArray();

    [Fact]
    public void RoundTripsWrappedPcm()
    {
        using var wav = WavFile.Wrap(Pcm(0, 16384, -32768, 32767), 16000, 1, 16);

        var samples = WavFile.ReadPcm16MonoAsFloat(wav);

        Assert.Equal([0f, 0.5f, -1f, 32767 / 32768f], samples);
    }

    [Fact]
    public void SkipsExtraChunksBeforeData()
    {
        using var original = WavFile.Wrap(Pcm(100, -100), 16000, 1, 16);
        var bytes = original.ToArray();
        // Insert an odd-sized "LIST" chunk (with its pad byte) between "fmt " and "data".
        var list = Encoding.ASCII.GetBytes("LIST").Concat(BitConverter.GetBytes(3)).Concat(new byte[] { 1, 2, 3, 0 });
        var patched = bytes[..36].Concat(list).Concat(bytes[36..]).ToArray();

        var samples = WavFile.ReadPcm16MonoAsFloat(new MemoryStream(patched));

        Assert.Equal([100 / 32768f, -100 / 32768f], samples);
    }

    [Fact]
    public void RejectsWrongFormat()
    {
        using var stereo = WavFile.Wrap(Pcm(1, 2), 44100, 2, 16);

        Assert.Throws<InvalidDataException>(() => WavFile.ReadPcm16MonoAsFloat(stereo));
    }

    [Fact]
    public void ToleratesDataSizeLargerThanFile()
    {
        var bytes = WavFile.Wrap(Pcm(7, 8, 9), 16000, 1, 16).ToArray();
        BitConverter.GetBytes(1_000_000).CopyTo(bytes, 40);

        Assert.Equal(3, WavFile.ReadPcm16MonoAsFloat(new MemoryStream(bytes)).Length);
    }
}
