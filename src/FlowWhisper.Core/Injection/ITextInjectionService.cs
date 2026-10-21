namespace FlowWhisper.Core.Injection;

public interface ITextInjectionService
{
    /// <summary>Inserts <paramref name="text"/> into whichever window currently has keyboard focus.</summary>
    /// <returns>"Clipboard" (pasted) or "Typed" (fallback), whichever path delivered the text.</returns>
    /// <exception cref="InvalidOperationException">Input was blocked; the text is left on the clipboard.</exception>
    Task<string> InjectTextAsync(string text);
}
