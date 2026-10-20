using System.Globalization;
using FlowWhisper.Core.Localization;

namespace FlowWhisper.Core.Tests;

public class LanguageResolverTests
{
    [Theory]
    [InlineData("sv-SE", "sv")]
    [InlineData("sv-FI", "sv")]
    [InlineData("en-US", "en")]
    [InlineData("de-DE", "en")]
    public void UiFollowsWindowsWithEnglishFallback(string windows, string expected) =>
        Assert.Equal(expected, LanguageResolver.ResolveUi("", new CultureInfo(windows)).Code);

    [Fact]
    public void UiOverrideWins() =>
        Assert.Equal("en", LanguageResolver.ResolveUi("en", new CultureInfo("sv-SE")).Code);

    [Theory]
    [InlineData("sv-SE", "sv")]
    [InlineData("en-GB", "en")]
    [InlineData("nb-NO", "no")]
    [InlineData("nn-NO", "nn")]
    [InlineData("ja-JP", "ja")]
    [InlineData("fil-PH", "tl")]
    [InlineData("zu-ZA", "auto")]
    public void DictationFollowsWindowsWhenSupported(string windows, string expected) =>
        Assert.Equal(expected, LanguageResolver.ResolveDictation("", new CultureInfo(windows)));

    [Fact]
    public void DictationListHasAllWhisperLanguagesOnceInAlphabeticalOrder()
    {
        var languages = LanguageResolver.DictationLanguages;
        Assert.Equal(100, languages.Select(l => l.Code).Distinct().Count());
        Assert.Equal(languages.Count, languages.Select(l => l.Code).Distinct().Count());

        var comparer = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
        for (var i = 1; i < languages.Count; i++)
        {
            Assert.True(comparer.Compare(languages[i - 1].NativeName, languages[i].NativeName) <= 0,
                $"{languages[i - 1].NativeName} before {languages[i].NativeName}");
        }
    }

    [Fact]
    public void DictationOverrideWins() =>
        Assert.Equal("auto", LanguageResolver.ResolveDictation("auto", new CultureInfo("sv-SE")));
}
