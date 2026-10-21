using System.Text.RegularExpressions;

namespace FlowWhisper.Core.Corrections;

/// <summary>
/// Rule-based handling of spoken self-corrections: "vi ses kl 12, nej förresten kl 13" →
/// "vi ses kl 13". A correction phrase is only acted on when what follows it is the same kind
/// of thing as something said just before (a time, a number, a weekday or a single word), so
/// an ordinary "nej" or "actually" in a sentence is left alone.
/// </summary>
public static partial class SelfCorrectionRules
{
    /// <summary>
    /// Phrases that take back what was just said. Multi-word ones first so "nej förresten"
    /// wins over "nej". The short, everyday ones only count after a comma or mid-sentence,
    /// never right after a full stop or question mark (see <see cref="LooseMarkers"/>).
    /// </summary>
    private static readonly string[] Markers =
    [
        "nej förresten", "nej vänta", "nej jag menar", "fast vänta", "eller nej", "eller snarare",
        "jag menar", "förlåt", "rättelse", "nej",
        "no wait", "scratch that", "or rather", "make that", "i mean", "sorry", "actually", "no",
    ];

    private static readonly HashSet<string> LooseMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "nej", "förlåt", "no", "sorry", "actually",
    };

    /// <summary>Broader cue words: any of these makes it plausible that something was taken back.</summary>
    public static readonly IReadOnlyList<string> CueWords =
        [.. Markers, "vänta", "förresten", "wait", "instead", "istället", "i stället"];

    private static readonly string[] Weekdays =
    [
        "måndag", "tisdag", "onsdag", "torsdag", "fredag", "lördag", "söndag",
        "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
    ];

    private const int MaxPasses = 3;

    // Hours as Whisper writes them: "13", "13:30", "13.30", or spelled out ("klockan ett", "at one").
    private const string Hour = @"(?<digits>\d{1,2}(?:[:.]\d{2})?(?!\d)|(?:ett|en|två|tre|fyra|fem|sex|sju|åtta|nio|tio|elva|tolv|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)(?![\p{L}]))";

    [GeneratedRegex(@"^(?:(?:kl\.?|klockan|at)\s*)?" + Hour, RegexOptions.IgnoreCase)]
    private static partial Regex TimeAtStart();

    [GeneratedRegex(@"\b(?:kl\.?|klockan|at)\s*" + Hour + @"|\b(?<digits>\d{1,2}[:.]\d{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex TimeAnywhere();

    [GeneratedRegex(@"\d+(?:[.,]\d+)?(?!\d)")]
    private static partial Regex Number();

    [GeneratedRegex(@"[\p{L}][\p{L}\p{Mn}'’-]*")]
    private static partial Regex Word();

    /// <summary>True when the text contains any cue that something may have been taken back.</summary>
    public static bool HasCue(string text) =>
        CueWords.Any(cue => Regex.IsMatch(text, $@"(?<![\p{{L}}]){Regex.Escape(cue)}(?![\p{{L}}])", RegexOptions.IgnoreCase));

    /// <summary>Applies every correction a rule can handle. Returns the text unchanged if none fit.</summary>
    public static string Apply(string text)
    {
        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var corrected = ApplyOnce(text);
            if (corrected is null)
            {
                break;
            }

            text = corrected;
        }

        return text;
    }

    private static string? ApplyOnce(string text)
    {
        foreach (var marker in FindMarkers(text))
        {
            var left = text[..marker.Start];
            var right = text[marker.End..];
            if (TryCorrect(left, right) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private readonly record struct MarkerHit(int Start, int End);

    private static IEnumerable<MarkerHit> FindMarkers(string text)
    {
        var alternation = string.Join("|", Markers.Select(Regex.Escape));
        // The marker must have something before it in the same text, and a word after it.
        var regex = new Regex($@"(?<before>[\p{{L}}\p{{N}}][,;:.!?–-]?)\s+(?<marker>{alternation})(?![\p{{L}}])[,;:.!?–-]*\s+(?=[\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase);
        foreach (Match match in regex.Matches(text))
        {
            var marker = match.Groups["marker"].Value;
            var before = match.Groups["before"].Value;
            if (LooseMarkers.Contains(marker) && before.Length > 1 && ".!?".Contains(before[^1]))
            {
                continue; // "…kl 12. Nej, …" starts a new sentence rather than correcting.
            }

            yield return new MarkerHit(match.Groups["before"].Index + 1, match.Index + match.Length);
        }
    }

    /// <summary>Replaces the last thing in <paramref name="left"/> that has the same kind as the start of <paramref name="right"/>.</summary>
    private static string? TryCorrect(string left, string right)
    {
        // A time: "kl 13", "13:30", "klockan ett", or a bare "13" when the left side said a time.
        var time = TimeAtStart().Match(right);
        var leftTimes = TimeAnywhere().Matches(left);
        if (time.Success && leftTimes.Count > 0)
        {
            var target = leftTimes[^1];
            var hasPrefix = time.Value.Length > time.Groups["digits"].Value.Length;
            var replaced = hasPrefix
                ? Splice(left, target.Index, target.Length, time.Value)
                : Splice(left, target.Groups["digits"].Index, target.Groups["digits"].Length, time.Groups["digits"].Value);
            return Join(replaced, right[time.Length..]);
        }

        // A number: "3", "2,5".
        var number = Number().Match(right);
        var leftNumbers = Number().Matches(left);
        if (number.Success && number.Index == 0 && leftNumbers.Count > 0)
        {
            var target = leftNumbers[^1];
            return Join(Splice(left, target.Index, target.Length, number.Value), right[number.Length..]);
        }

        var firstWord = Word().Match(right);
        if (!firstWord.Success || firstWord.Index != 0)
        {
            return null;
        }

        // A weekday: "tisdag, nej onsdag".
        if (WeekdayStem(firstWord.Value) is not null)
        {
            var target = Word().Matches(left).LastOrDefault(w => WeekdayStem(w.Value) is not null);
            return target is null ? null : Join(Splice(left, target.Index, target.Length, firstWord.Value), right[firstWord.Length..]);
        }

        // A single word or name, standing on its own up to the end or a punctuation mark:
        // "till Anna, nej Erik", "ta bilen, nej tåget". Longer phrases need the model.
        var (phrase, rest) = LeadingPhrase(right);
        var phraseWords = Word().Matches(phrase);
        if (phraseWords.Count != 1)
        {
            return null;
        }

        var leftWords = Word().Matches(left);
        if (leftWords.Count < 2)
        {
            return null;
        }

        var replacement = phraseWords[0];
        var candidate = char.IsUpper(replacement.Value[0])
            // A name replaces the last capitalized word that isn't simply starting the text.
            ? leftWords.Skip(1).LastOrDefault(w => char.IsUpper(w.Value[0]))
            : leftWords[^1];
        if (candidate is null)
        {
            return null;
        }

        return Join(Splice(left, candidate.Index, candidate.Length, replacement.Value), rest);
    }

    private static string? WeekdayStem(string word) =>
        Weekdays.FirstOrDefault(d => word.StartsWith(d, StringComparison.OrdinalIgnoreCase)
            && word.Length - d.Length <= 2);

    /// <summary>Splits off the words up to the first punctuation mark (or the end).</summary>
    private static (string Phrase, string Tail) LeadingPhrase(string right)
    {
        var end = right.IndexOfAny([',', ';', ':', '.', '!', '?', '–']);
        return end < 0 ? (right, string.Empty) : (right[..end], right[end..]);
    }

    private static string Splice(string text, int index, int length, string replacement) =>
        string.Concat(text.AsSpan(0, index), replacement, text.AsSpan(index + length));

    /// <summary>Joins the corrected left side and what followed the replacement, without a dangling comma.</summary>
    private static string Join(string left, string rest)
    {
        left = left.TrimEnd(' ', ',', ';', ':', '–', '-');
        if (rest.Length == 0)
        {
            return left;
        }

        return char.IsPunctuation(rest[0]) && rest[0] is not '–' and not '-'
            ? left + rest.TrimEnd()
            : left + " " + rest.Trim();
    }
}
