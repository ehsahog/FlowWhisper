using System.Text.RegularExpressions;

namespace FlowWhisper.Core.Cleanup;

/// <summary>
/// Decides whether an LLM "cleaned" transcript is still faithful to what was
/// actually said. Small local models were measured to garble Swedish, drop
/// information, and even answer dictated questions instead of copying them,
/// so their output is only accepted when it adds no words and keeps nearly
/// every non-filler word of the original.
/// </summary>
public static partial class TranscriptGuard
{
    public static readonly IReadOnlySet<string> HesitationSounds =
        new HashSet<string>(StringComparer.Ordinal) { "eh", "öh", "ehm", "äh", "hmm" };

    private const double MinKeptRatio = 0.9;

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRegex();

    public static bool IsFaithful(string raw, string cleaned)
    {
        var source = Words(raw).Where(w => !HesitationSounds.Contains(w)).ToList();
        var output = Words(cleaned).ToList();
        if (output.Count == 0)
        {
            return false;
        }

        var sourceSet = source.ToHashSet(StringComparer.Ordinal);
        var outputSet = output.ToHashSet(StringComparer.Ordinal);

        var addedWords = output.Count(w => !sourceSet.Contains(w));
        var keptWords = source.Count(outputSet.Contains);

        return addedWords == 0 && keptWords >= MinKeptRatio * source.Count;
    }

    private static IEnumerable<string> Words(string text) =>
        WordRegex().Matches(text).Select(m => m.Value.ToLowerInvariant());
}
