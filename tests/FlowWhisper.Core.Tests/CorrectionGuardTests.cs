using FlowWhisper.Core.Corrections;

namespace FlowWhisper.Core.Tests;

public class CorrectionGuardTests
{
    [Theory]
    // Accepted: one run removed, ending with the cue, everything after the cue kept.
    [InlineData("vi ses kl 12, nej förresten kl 13", "Vi ses kl 13.", true)]
    [InlineData("skicka till Anna, jag menar Erik", "Skicka till Erik.", true)]
    [InlineData("Skicka offerten i dag, eller snarare i morgon förmiddag.", "Skicka offerten i morgon förmiddag", true)]
    [InlineData("Let's meet on Tuesday, no wait, Thursday works better.", "Let's meet on Thursday works better", true)]
    [InlineData("vi har tre katter och en hund", "Vi har tre katter och en hund.", true)]                 // unchanged
    // Rejected: measured Phi-3.5 mistakes and other unsafe shapes.
    [InlineData("Vi ses klockan tolv, nej förresten klockan ett på kontoret.", "vi ses klockan tolv, på kontoret", false)] // kept the wrong part
    [InlineData("Vi tar bussen i morgon, fast vänta, tåget går snabbare.", "Vi tar bussen i morgon, tåget går snabbare", false)] // removed only the cue
    [InlineData("Boka det lilla rummet, nej vänta, det stora rummet.", "Boka det lilla rummet, det stora rummet, nej vänta", false)] // reordered
    [InlineData("vi har tre katter och en hund", "Vi har tre katter.", false)]                            // deletion without a cue
    [InlineData("kan du förklara hur fotosyntes fungerar", "Fotosyntes omvandlar ljus.", false)]         // an answer
    [InlineData("vi tar tisdag, fast vänta, onsdag passar", "Vi tar onsdag, det passar.", false)]          // added a word
    public void AcceptsOnlyTheShapeOfACorrection(string dictated, string corrected, bool expected) =>
        Assert.Equal(expected, CorrectionGuard.IsAcceptable(dictated, corrected));
}
