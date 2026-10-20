using System.Text.RegularExpressions;

namespace FlowWhisper.Core.Transcription;

public sealed record TranscriptSegment(string Text, float NoSpeechProbability);

/// <summary>
/// Removes Whisper's two most visible failure modes before text is typed into
/// another app: non-speech annotations for silence/noise (observed:
/// "[Tillagets ljud]"), and hallucinated repetition loops (observed: one
/// sentence seven times in a row).
/// </summary>
/// <remarks>
/// Segments are deliberately not dropped on no-speech probability alone: whisper.cpp
/// only trusts it combined with a low log-probability, and on its own it can discard
/// real speech. Fully silent recordings are rejected before transcription instead.
/// </remarks>
public static partial class TranscriptPostProcessor
{
    [GeneratedRegex(@"\[[^\]]*\]|♪+")]
    private static partial Regex AnnotationRegex();

    [GeneratedRegex(@"^\s*\([^)]*\)\s*$")]
    private static partial Regex WholeParenthesizedRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplitRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiSpaceRegex();

    /// <summary>
    /// Subtitle credits Whisper invents in pauses, learned from subtitled video in its
    /// training data (observed here: "Tack till elever och personal vid Svanskylland.").
    /// Matched per sentence, so a real sentence elsewhere in the dictation survives.
    /// </summary>
    [GeneratedRegex(
        @"^(tack (för att (du|ni) (har )?(tittat|tittade|lyssnat|lyssnade))|tack till elever och personal|" +
        @"svensktextning|(svensk )?textning( av| från| och)?\b|undertext(er|ning)? (av|från)|" +
        @"prenumerera på (kanalen|min kanal)|thanks? (you )?for watching|subtitles by|transcribed by)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CreditRegex();

    public static string Process(IEnumerable<TranscriptSegment> segments)
    {
        var kept = new List<string>();
        foreach (var segment in segments)
        {
            var text = AnnotationRegex().Replace(segment.Text, " ").Trim();
            if (text.Length == 0 || WholeParenthesizedRegex().IsMatch(text))
            {
                continue;
            }

            kept.Add(text);
        }

        var sentences = kept
            .SelectMany(t => SentenceSplitRegex().Split(t))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0 && !CreditRegex().IsMatch(s));

        return MultiSpaceRegex().Replace(string.Join(' ', CollapseRepeats(sentences)), " ").Trim();
    }

    /// <summary>Drops a sentence that is identical to the one right before it.</summary>
    private static IEnumerable<string> CollapseRepeats(IEnumerable<string> sentences)
    {
        string? previous = null;
        foreach (var sentence in sentences)
        {
            var key = Normalize(sentence);
            if (key != previous)
            {
                yield return sentence;
            }

            previous = key;
        }
    }

    private static string Normalize(string sentence) =>
        new string(sentence.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
