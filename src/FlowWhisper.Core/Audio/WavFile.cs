using System.Text;

namespace FlowWhisper.Core.Audio;

public static class WavFile
{
    /// <summary>Wraps raw PCM in a canonical 44-byte RIFF/WAVE header.</summary>
    public static MemoryStream Wrap(byte[] pcm, int sampleRate, int channels, int bitsPerSample)
    {
        var blockAlign = channels * bitsPerSample / 8;
        var stream = new MemoryStream(44 + pcm.Length);
        using (var w = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            w.Write("RIFF"u8);
            w.Write(36 + pcm.Length);
            w.Write("WAVE"u8);
            w.Write("fmt "u8);
            w.Write(16);
            w.Write((short)1);
            w.Write((short)channels);
            w.Write(sampleRate);
            w.Write(sampleRate * blockAlign);
            w.Write((short)blockAlign);
            w.Write((short)bitsPerSample);
            w.Write("data"u8);
            w.Write(pcm.Length);
            w.Write(pcm);
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Reads a 16 kHz mono PCM16 WAV into samples in [-1, 1], the input format of
    /// OpenVINO's Whisper pipeline. Walks the RIFF chunks rather than assuming a
    /// 44-byte header, since writers may add chunks (e.g. "LIST") before "data".
    /// </summary>
    public static float[] ReadPcm16MonoAsFloat(Stream wav)
    {
        using var r = new BinaryReader(wav, Encoding.ASCII, leaveOpen: true);
        if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "RIFF")
        {
            throw new InvalidDataException("Not a RIFF/WAVE file.");
        }

        r.ReadInt32();
        if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "WAVE")
        {
            throw new InvalidDataException("Not a RIFF/WAVE file.");
        }

        var formatChecked = false;
        while (wav.Position + 8 <= wav.Length)
        {
            var id = Encoding.ASCII.GetString(r.ReadBytes(4));
            var size = r.ReadInt32();
            if (id == "fmt ")
            {
                var format = r.ReadInt16();
                var channels = r.ReadInt16();
                var sampleRate = r.ReadInt32();
                r.ReadInt32();
                r.ReadInt16();
                var bits = r.ReadInt16();
                if (format != 1 || channels != 1 || sampleRate != 16000 || bits != 16)
                {
                    throw new InvalidDataException(
                        $"Expected 16 kHz mono PCM16, got format {format}, {channels} ch, {sampleRate} Hz, {bits}-bit.");
                }

                wav.Seek(size - 16 + (size & 1), SeekOrigin.Current);
                formatChecked = true;
            }
            else if (id == "data")
            {
                if (!formatChecked)
                {
                    throw new InvalidDataException("WAV data chunk appears before its format chunk.");
                }

                // A writer that was still recording may report a size larger than what's on disk.
                var available = (int)Math.Min(size, wav.Length - wav.Position) / 2;
                var bytes = r.ReadBytes(available * 2);
                var samples = new float[available];
                for (var i = 0; i < available; i++)
                {
                    samples[i] = BitConverter.ToInt16(bytes, i * 2) / 32768f;
                }

                return samples;
            }
            else
            {
                wav.Seek(size + (size & 1), SeekOrigin.Current);
            }
        }

        throw new InvalidDataException("WAV file has no data chunk.");
    }
}
