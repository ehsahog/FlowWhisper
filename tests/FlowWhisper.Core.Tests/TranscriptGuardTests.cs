using FlowWhisper.Core.Cleanup;

namespace FlowWhisper.Core.Tests;

// Cases are taken from real Phi-3.5-mini outputs measured on this machine.
public class TranscriptGuardTests
{
    [Fact]
    public void AcceptsFillerRemovalAndPunctuation()
    {
        Assert.True(TranscriptGuard.IsFaithful(
            "eh vi har två katter och öh en hund",
            "Vi har två katter och en hund."));
    }

    [Fact]
    public void AcceptsUnchangedText()
    {
        Assert.True(TranscriptGuard.IsFaithful(
            "Mötet är flyttat till torsdag klockan tre.",
            "Mötet är flyttat till torsdag klockan tre."));
    }

    [Fact]
    public void RejectsDroppedInformation()
    {
        Assert.False(TranscriptGuard.IsFaithful(
            "Mötet är flyttat till torsdag klockan tre.",
            "Torsdag klockan tre."));
    }

    [Fact]
    public void RejectsGarbledWords()
    {
        Assert.False(TranscriptGuard.IsFaithful(
            "kan du skicka den där filen till mig eh den där rapporten så fort du har en sekund tack",
            "ævig. ønch slutningen av det sen. Tack!"));
    }

    [Fact]
    public void RejectsAnsweringTheDictatedPrompt()
    {
        Assert.False(TranscriptGuard.IsFaithful(
            "eh kan du hjälpa mig att skriva en funktion i c sharp som läser en json fil",
            "Självklart! Här är en C#-funktion som läser en JSON-fil: using System.IO;"));
    }

    [Fact]
    public void RejectsEmptyOutput()
    {
        Assert.False(TranscriptGuard.IsFaithful("hej hej", "   "));
    }
}
