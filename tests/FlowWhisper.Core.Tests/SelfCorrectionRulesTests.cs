using FlowWhisper.Core.Corrections;

namespace FlowWhisper.Core.Tests;

public class SelfCorrectionRulesTests
{
    [Theory]
    // Times
    [InlineData("Vi ses kl 12, nej förresten kl 13.", "Vi ses kl 13.")]
    [InlineData("Vi ses kl 12, nej förresten 13 på kontoret.", "Vi ses kl 13 på kontoret.")]
    [InlineData("Mötet börjar 14:30, förlåt, 15:00 i stora rummet.", "Mötet börjar 15:00 i stora rummet.")]
    [InlineData("Let's meet at 12, no wait, at 1 in the lobby.", "Let's meet at 1 in the lobby.")]
    [InlineData("Vi ses klockan tolv, nej förresten klockan ett på kontoret.", "Vi ses klockan ett på kontoret.")]
    [InlineData("Vi ses klockan tolv, nej ett.", "Vi ses klockan ett.")]
    [InlineData("Call me at eleven, sorry, at twelve.", "Call me at twelve.")]
    // Numbers
    [InlineData("Köp 3 liter mjölk, nej 4.", "Köp 4 liter mjölk.")]
    [InlineData("The budget is 200, I mean 250 dollars.", "The budget is 250 dollars.")]
    // Weekdays
    [InlineData("Vi kör på tisdag, nej vänta, onsdag.", "Vi kör på onsdag.")]
    [InlineData("Send it on Monday, sorry, Tuesday.", "Send it on Tuesday.")]
    // Names and single words
    [InlineData("Skicka rapporten till Anna, jag menar Erik, innan fredag.", "Skicka rapporten till Erik, innan fredag.")]
    [InlineData("Vi tar bilen, nej tåget.", "Vi tar tåget.")]
    [InlineData("Jag vill ha te, eller nej, kaffe.", "Jag vill ha kaffe.")]
    public void AppliesCorrections(string input, string expected) =>
        Assert.Equal(expected, SelfCorrectionRules.Apply(input));

    [Theory]
    [InlineData("Nej, det tror jag inte.")]                              // "nej" as an answer
    [InlineData("Han sa nej, men vi försöker igen.")]                    // followed by a phrase, not a replacement
    [InlineData("Ses vi kl 12? Nej, det passar inte.")]                 // new sentence, no matching kind
    [InlineData("I actually think the plan is good.")]                  // "actually" as an adverb
    [InlineData("Förlåt att jag är sen.")]                              // marker at the very start
    [InlineData("Vi har tre katter och en hund.")]                      // no marker at all
    [InlineData("Jag menar att vi borde vänta till hösten.")]
    public void LeavesOrdinarySentencesAlone(string input) =>
        Assert.Equal(input, SelfCorrectionRules.Apply(input));

    [Theory]
    [InlineData("vi tar tisdag, fast vänta, onsdag passar nog bättre", true)]
    [InlineData("let's do it, no wait, tomorrow is better", true)]
    [InlineData("vi har tre katter och en hund", false)]
    public void DetectsCorrectionCues(string input, bool expected) =>
        Assert.Equal(expected, SelfCorrectionRules.HasCue(input));
}
