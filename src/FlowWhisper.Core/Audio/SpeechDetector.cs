namespace FlowWhisper.Core.Audio;

/// <summary>
/// Cheap energy-based check for "did anyone actually speak?" before running Whisper.
/// Whisper given a quiet room invents subtitle credits (observed: "Tack till elever och
/// personal vid Svanskylland."), so silence must never reach it.
/// </summary>
/// <remarks>
/// The room's noise floor is estimated per recording from its quietest frames, so the
/// same rule works in a silent office and next to a fan, and for a distant mic: measured
/// speech from the Surface mic across a desk peaked at 0.04–0.07 against an ambient
/// floor around 0.002.
/// </remarks>
public static class SpeechDetector
{
    private const int SampleRate = 16000;
    private const int FrameSamples = SampleRate / 50; // 20 ms
    private const double FloorPercentile = 0.2;
    private const double FloorMultiple = 4.0;
    private const double AbsoluteMinRms = 0.004;
    private static readonly TimeSpan MinSpeech = TimeSpan.FromMilliseconds(250);

    public static bool ContainsSpeech(ReadOnlySpan<float> samples)
    {
        var frameCount = samples.Length / FrameSamples;
        if (frameCount == 0)
        {
            return false;
        }

        var rms = new double[frameCount];
        for (var f = 0; f < frameCount; f++)
        {
            double sum = 0;
            var frame = samples.Slice(f * FrameSamples, FrameSamples);
            foreach (var s in frame)
            {
                sum += s * s;
            }

            rms[f] = Math.Sqrt(sum / FrameSamples);
        }

        var sorted = rms.Order().ToArray();
        var floor = sorted[(int)(FloorPercentile * (sorted.Length - 1))];
        var threshold = Math.Max(floor * FloorMultiple, AbsoluteMinRms);

        var speechFrames = rms.Count(r => r > threshold);
        return speechFrames * 20 >= MinSpeech.TotalMilliseconds;
    }
}
