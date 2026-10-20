using System.Globalization;

namespace FlowWhisper.Core.Localization;

/// <summary>Turns "follow Windows" settings into concrete languages.</summary>
public static class LanguageResolver
{
    /// <summary>
    /// Every language Whisper large-v3 transcribes (the codes are its own, e.g. "jw" for
    /// Javanese), shown by their own names in alphabetical order. Cantonese ("yue") only
    /// exists in large-v3; the small model falls back to automatic detection for it.
    /// </summary>
    public static readonly IReadOnlyList<(string Code, string NativeName)> DictationLanguages =
        new (string Code, string NativeName)[]
        {
            ("af", "Afrikaans"), ("am", "አማርኛ"), ("ar", "العربية"), ("as", "অসমীয়া"),
            ("az", "Azərbaycan"), ("ba", "Башҡорт"), ("be", "Беларуская"), ("bg", "Български"),
            ("bn", "বাংলা"), ("bo", "བོད་སྐད་"), ("br", "Brezhoneg"), ("bs", "Bosanski"),
            ("ca", "Català"), ("cs", "Čeština"), ("cy", "Cymraeg"), ("da", "Dansk"),
            ("de", "Deutsch"), ("el", "Ελληνικά"), ("en", "English"), ("es", "Español"),
            ("et", "Eesti"), ("eu", "Euskara"), ("fa", "فارسی"), ("fi", "Suomi"),
            ("fo", "Føroyskt"), ("fr", "Français"), ("gl", "Galego"), ("gu", "ગુજરાતી"),
            ("ha", "Hausa"), ("haw", "ʻŌlelo Hawaiʻi"), ("he", "עברית"), ("hi", "हिन्दी"),
            ("hr", "Hrvatski"), ("ht", "Kreyòl ayisyen"), ("hu", "Magyar"), ("hy", "Հայերեն"),
            ("id", "Bahasa Indonesia"), ("is", "Íslenska"), ("it", "Italiano"), ("ja", "日本語"),
            ("jw", "Basa Jawa"), ("ka", "ქართული"), ("kk", "Қазақ тілі"), ("km", "ខ្មែរ"),
            ("kn", "ಕನ್ನಡ"), ("ko", "한국어"), ("la", "Latina"), ("lb", "Lëtzebuergesch"),
            ("ln", "Lingála"), ("lo", "ລາວ"), ("lt", "Lietuvių"), ("lv", "Latviešu"),
            ("mg", "Malagasy"), ("mi", "Māori"), ("mk", "Македонски"), ("ml", "മലയാളം"),
            ("mn", "Монгол"), ("mr", "मराठी"), ("ms", "Bahasa Melayu"), ("mt", "Malti"),
            ("my", "ဗမာ"), ("ne", "नेपाली"), ("nl", "Nederlands"), ("nn", "Norsk nynorsk"),
            ("no", "Norsk"), ("oc", "Occitan"), ("pa", "ਪੰਜਾਬੀ"), ("pl", "Polski"),
            ("ps", "پښتو"), ("pt", "Português"), ("ro", "Română"), ("ru", "Русский"),
            ("sa", "संस्कृतम्"), ("sd", "سنڌي"), ("si", "සිංහල"), ("sk", "Slovenčina"),
            ("sl", "Slovenščina"), ("sn", "ChiShona"), ("so", "Soomaali"), ("sq", "Shqip"),
            ("sr", "Srpski"), ("su", "Basa Sunda"), ("sv", "Svenska"), ("sw", "Kiswahili"),
            ("ta", "தமிழ்"), ("te", "తెలుగు"), ("tg", "Тоҷикӣ"), ("th", "ไทย"),
            ("tk", "Türkmen dili"), ("tl", "Tagalog"), ("tr", "Türkçe"), ("tt", "Татар"),
            ("uk", "Українська"), ("ur", "اردو"), ("uz", "Oʻzbek"), ("vi", "Tiếng Việt"),
            ("yi", "ייִדיש"), ("yo", "Èdè Yorùbá"), ("yue", "粵語"), ("zh", "中文"),
        }
        .OrderBy(l => l.NativeName, StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true))
        .ToArray();

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

        // Map Windows' ISO codes to Whisper's where they differ.
        var code = windowsUiCulture.TwoLetterISOLanguageName switch
        {
            "nb" => "no",
            "jv" => "jw",
            "fil" => "tl",
            var other => other,
        };
        return DictationLanguages.Any(l => l.Code == code) ? code : "auto";
    }
}
