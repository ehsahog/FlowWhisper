using System.Globalization;

namespace FlowWhisper.Core.Localization;

/// <summary>Turns "follow Windows" settings into concrete languages.</summary>
public static class LanguageResolver
{
    /// <summary>Whisper language codes offered in Settings, shown by their own names.</summary>
    public static readonly IReadOnlyList<(string Code, string NativeName)> DictationLanguages =
    [
        ("sv", "Svenska"),
        ("en", "English"),
        ("no", "Norsk"),
        ("da", "Dansk"),
        ("fi", "Suomi"),
        ("de", "Deutsch"),
        ("fr", "Français"),
        ("es", "Español"),
    ];

    /// <summary>
    /// The UI is translated to Swedish and English. Empty setting = follow Windows' display
    /// language: Swedish Windows gets Swedish, anything else gets English.
    /// </summary>
    public static UiStrings ResolveUi(string? setting, CultureInfo windowsUiCulture) =>
        (string.IsNullOrEmpty(setting) ? windowsUiCulture.TwoLetterISOLanguageName : setting) == "sv"
            ? UiStrings.Swedish
            : UiStrings.English;

    /// <summary>
    /// Empty setting = the Windows display language if Whisper supports it in the list,
    /// otherwise automatic detection. Norwegian Windows reports "nb"/"nn"; Whisper calls it "no".
    /// </summary>
    public static string ResolveDictation(string? setting, CultureInfo windowsUiCulture)
    {
        if (!string.IsNullOrEmpty(setting))
        {
            return setting;
        }

        var code = windowsUiCulture.TwoLetterISOLanguageName switch
        {
            "nb" or "nn" => "no",
            var other => other,
        };
        return DictationLanguages.Any(l => l.Code == code) ? code : "auto";
    }
}
