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
    [InlineData("ja-JP", "auto")]
    public void DictationFollowsWindowsWhenSupported(string windows, string expected) =>
        Assert.Equal(expected, LanguageResolver.ResolveDictation("", new CultureInfo(windows)));

    [Fact]
    public void DictationOverrideWins() =>
        Assert.Equal("auto", LanguageResolver.ResolveDictation("auto", new CultureInfo("sv-SE")));
}
