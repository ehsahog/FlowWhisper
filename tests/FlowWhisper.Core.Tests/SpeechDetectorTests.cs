using FlowWhisper.Core.Audio;

namespace FlowWhisper.Core.Tests;

public class SpeechDetectorTests
{
    private const int Rate = 16000;
    private static readonly Random Rng = new(42);

    private static float[] Noise(double seconds, double amplitude) =>
        Enumerable.Range(0, (int)(seconds * Rate)).Select(_ => (float)((Rng.NextDouble() * 2 - 1) * amplitude)).ToArray();

    /// <summary>A voiced-like tone burst on top of room noise, at the levels measured on this machine.</summary>
    private static float[] WithSpeech(float[] noise, double startSec, double durationSec, double amplitude)
    {
        var result = (float[])noise.Clone();
        var start = (int)(startSec * Rate);
        for (var i = 0; i < (int)(durationSec * Rate) && start + i < result.Length; i++)
        {
            result[start + i] += (float)(amplitude * Math.Sin(2 * Math.PI * 180 * i / Rate));
        }

        return result;
    }

    [Fact]
    public void QuietRoomIsNotSpeech() => Assert.False(SpeechDetector.ContainsSpeech(Noise(3, 0.003)));

    [Fact]
    public void NoisyRoomIsNotSpeech() => Assert.False(SpeechDetector.ContainsSpeech(Noise(3, 0.03)));

    [Fact]
    public void DigitalSilenceIsNotSpeech() => Assert.False(SpeechDetector.ContainsSpeech(new float[Rate * 2]));

    [Fact]
    public void DistantSpeechInQuietRoomIsSpeech() =>
        Assert.True(SpeechDetector.ContainsSpeech(WithSpeech(Noise(4, 0.003), 1, 1.5, 0.05)));

    [Fact]
    public void SpeechInNoisyRoomIsSpeech() =>
        Assert.True(SpeechDetector.ContainsSpeech(WithSpeech(Noise(4, 0.02), 1, 1.5, 0.2)));

    [Fact]
    public void ClickShorterThanASyllableIsNotSpeech() =>
        Assert.False(SpeechDetector.ContainsSpeech(WithSpeech(Noise(3, 0.003), 1, 0.1, 0.3)));
}
