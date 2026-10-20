using FlowWhisper.Core.Transcription;

namespace FlowWhisper.Core.Tests;

// Inputs are real Whisper outputs recorded by the app on this machine.
public class TranscriptPostProcessorTests
{
    private static string Run(params (string Text, float NoSpeech)[] segments) =>
        TranscriptPostProcessor.Process(segments.Select(s => new TranscriptSegment(s.Text, s.NoSpeech)));

    [Fact]
    public void DropsBracketedNonSpeechAnnotation()
    {
        Assert.Equal("", Run((" [Tillagets ljud]", 0.1f)));
    }

    [Fact]
    public void KeepsSpeechEvenWhenWhisperRatesItLikelySilence()
    {
        Assert.Equal("Hej. Vi ses.", Run((" Hej.", 0.05f), (" Vi ses.", 0.92f)));
    }

    [Fact]
    public void CollapsesRepetitionLoop()
    {
        var loop = string.Concat(Enumerable.Repeat("Det är en av de här kvällsvartier. ", 7));
        Assert.Equal("Det är en av de här kvällsvartier.", Run((loop, 0.1f)));
    }

    [Fact]
    public void CollapsesRepeatsAcrossSegments()
    {
        Assert.Equal("Vi ses imorgon.", Run((" Vi ses imorgon.", 0.1f), (" vi ses imorgon", 0.1f)));
    }

    [Fact]
    public void KeepsNonConsecutiveRepeatsAndNormalText()
    {
        Assert.Equal(
            "Ja. Vi tar det på mötet. Ja.",
            Run((" Ja. Vi tar det på mötet.", 0.1f), (" Ja.", 0.1f)));
    }

    [Theory]
    [InlineData(" Tack till elever och personal vid Svanskylland.")]
    [InlineData(" Tack för att du tittade!")]
    [InlineData(" Svensktextning.nu")]
    [InlineData(" Textning: Karin Andersson")]
    [InlineData(" Thanks for watching!")]
    public void DropsSubtitleCreditHallucinations(string text) => Assert.Equal("", Run((text, 0.1f)));

    [Fact]
    public void KeepsRealSentenceNextToACredit() =>
        Assert.Equal("Skicka rapporten idag.", Run((" Skicka rapporten idag. Tack för att du tittade.", 0.1f)));

    [Fact]
    public void KeepsOrdinaryThanks() =>
        Assert.Equal("Tack för hjälpen med rapporten.", Run((" Tack för hjälpen med rapporten.", 0.1f)));

    [Fact]
    public void DropsWholeParenthesizedSegmentButKeepsInlineParentheses()
    {
        Assert.Equal(
            "Skicka rapporten (den nya) idag.",
            Run((" (musik)", 0.2f), (" Skicka rapporten (den nya) idag.", 0.1f)));
    }
}
