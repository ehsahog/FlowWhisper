namespace FlowWhisper.Core;

/// <summary>
/// Single source of truth for where FlowWhisper keeps its data. Setting the
/// FLOWWHISPER_DATA_DIR environment variable points everything elsewhere —
/// used to test a true first-run install without touching real data.
/// </summary>
public static class AppPaths
{
    public static string Root { get; } =
        Environment.GetEnvironmentVariable("FLOWWHISPER_DATA_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowWhisper");

    public static string Models => Path.Combine(Root, "models");
    public static string Runtime => Path.Combine(Root, "runtime");
    public static string Cache => Path.Combine(Root, "cache");
    public static string Data => Path.Combine(Root, "data");
    public static string Logs => Path.Combine(Root, "logs");
    public static string Recordings => Path.Combine(Root, "recordings");
}
