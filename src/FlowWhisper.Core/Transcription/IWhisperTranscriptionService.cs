namespace FlowWhisper.Core.Transcription;

public interface IWhisperTranscriptionService
{
    /// <param name="wavPath">Path to a 16kHz mono PCM16 WAV file.</param>
    /// <param name="language">ISO 639-1 code (e.g. "sv"), or "auto" to detect.</param>
    Task<string> TranscribeAsync(string wavPath, string language, CancellationToken cancellationToken = default);

    /// <param name="wav">A 16kHz mono PCM16 WAV stream, e.g. a live snapshot of an ongoing recording.</param>
    Task<string> TranscribeAsync(Stream wav, string language, CancellationToken cancellationToken = default);
}
