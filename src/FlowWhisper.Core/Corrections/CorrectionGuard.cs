using System.Text.RegularExpressions;

namespace FlowWhisper.Core.Corrections;

/// <summary>
/// Accepts a model's self-correction only if it has the shape of one: a single run of words
/// was deleted, that run ends exactly with a correction phrase ("nej förresten", "no wait"),
/// and it starts before the phrase. Everything said after the phrase is kept, everything
/// before the deleted run too, in order and unchanged. Measured with Phi-3.5: without this
/// shape check the model sometimes kept the part that was taken back ("kl 12") and dropped
/// the replacement, which a plain "only deletions" check let through.
/// </summary>
public static partial class CorrectionGuard
{
    /// <summary>Most words a single correction may remove (the taken-back part plus the phrase).</summary>
    private const int MaxDeletedWords = 12;

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRegex();

    public static bool IsAcceptable(string dictated, string corrected)
    {
        var source = Words(dictated);
        var output = Words(corrected);
        if (output.Count == 0)
        {
            return false;
        }

        if (output.SequenceEqual(source))
        {
            return true; // unchanged (punctuation or case may differ)
        }

        var removed = source.Count - output.Count;
        if (removed <= 0 || removed > MaxDeletedWords)
        {
            return false;
        }

        foreach (var (cueStart, cueEnd) in CueSpans(source))
        {
            // Deleted run = source[runStart .. cueEnd): it must start before the cue.
            var runStart = cueEnd - removed;
            if (runStart < 0 || runStart >= cueStart)
            {
                continue;
            }

            if (source.Take(runStart).Concat(source.Skip(cueEnd)).SequenceEqual(output))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Word index ranges [start, end) of every correction cue in the dictation. A cue inside a
    /// longer one ("vänta" in "fast vänta") is dropped: the taken-back part must come before
    /// the whole phrase, not just before its last word.
    /// </summary>
    private static List<(int Start, int End)> CueSpans(List<string> words)
    {
        var spans = new List<(int Start, int End)>();
        foreach (var cue in SelfCorrectionRules.CueWords)
        {
            var cueWords = Words(cue);
            for (var i = 0; i + cueWords.Count <= words.Count; i++)
            {
                if (words.Skip(i).Take(cueWords.Count).SequenceEqual(cueWords))
                {
                    spans.Add((i, i + cueWords.Count));
                }
            }
        }

        return spans
            .Where(s => !spans.Any(o => o != s && o.Start <= s.Start && o.End >= s.End))
            .ToList();
    }

    private static List<string> Words(string text) =>
        WordRegex().Matches(text).Select(m => m.Value.ToLowerInvariant()).ToList();
}
