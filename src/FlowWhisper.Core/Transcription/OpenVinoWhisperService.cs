using FlowWhisper.Core.Audio;
using FlowWhisper.Core.Cleanup;

namespace FlowWhisper.Core.Transcription;

/// <summary>
/// Whisper via OpenVINO GenAI on GPU/NPU/CPU. Measured on this machine with a real
/// 15 s Swedish recording: large-v3-turbo 1.44 s on GPU, small 0.66 s on NPU — versus
/// 5.7 s for whisper.cpp (Whisper.net) small on CPU.
/// </summary>
public sealed class OpenVinoWhisperService : IWhisperTranscriptionService, IDisposable
{
    // The pipeline isn't thread-safe and a generate call can't be interrupted, so calls are
    // serialized; Dispose waits for an in-flight call instead of freeing native memory under it.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private nint _pipeline;

    /// <summary>Language codes this model knows, e.g. small lacks "yue" that large-v3 has.</summary>
    private readonly HashSet<string> _languages;

    public string Device { get; }

    public OpenVinoWhisperService(string modelDirectory, string device, string runtimeDirectory, string cacheDirectory)
    {
        Device = device;
        _languages = ReadModelLanguages(modelDirectory);
        OpenVinoGenAi.Initialize(runtimeDirectory);
        Directory.CreateDirectory(cacheDirectory);
        OpenVinoGenAi.Check(
            OpenVinoGenAi.WhisperCreate(modelDirectory, device, 2, out _pipeline, "CACHE_DIR", cacheDirectory),
            $"whisper_pipeline_create({device})");
    }

    public async Task<string> TranscribeAsync(string wavPath, string language, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(wavPath);
        return await TranscribeAsync(stream, language, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> TranscribeAsync(Stream wav, string language, CancellationToken cancellationToken = default)
    {
        var samples = WavFile.ReadPcm16MonoAsFloat(wav);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_pipeline == 0, this);
            var text = await Task.Run(() => Generate(samples, language), CancellationToken.None).ConfigureAwait(false);
            return TranscriptPostProcessor.Process([new TranscriptSegment(text, 0f)]);
        }
        finally
        {
            _gate.Release();
        }
    }

    private unsafe string Generate(float[] samples, string language)
    {
        nint config = 0, results = 0;
        try
        {
            // Start from the model's own config: it carries the language/task token maps.
            OpenVinoGenAi.Check(OpenVinoGenAi.WhisperGetConfig(_pipeline, out config), "whisper_get_generation_config");
            // A language the model doesn't know would fail the whole call; detect instead.
            if (language != "auto" && _languages.Contains(language))
            {
                OpenVinoGenAi.Check(OpenVinoGenAi.WhisperConfigSetLanguage(config, $"<|{language}|>"), "whisper_set_language");
            }

            OpenVinoGenAi.Check(OpenVinoGenAi.WhisperConfigSetTask(config, "transcribe"), "whisper_set_task");

            fixed (float* p = samples)
            {
                OpenVinoGenAi.Check(OpenVinoGenAi.WhisperGenerate(_pipeline, p, (nuint)samples.Length, config, out results),
                    "whisper_pipeline_generate");
            }

            return OpenVinoGenAi.ReadWhisperResultString(results);
        }
        finally
        {
            if (results != 0) OpenVinoGenAi.WhisperResultsFree(results);
            if (config != 0) OpenVinoGenAi.WhisperConfigFree(config);
        }
    }

    /// <summary>Reads the "&lt;|sv|&gt;"-style keys of lang_to_id in the model's generation_config.json.</summary>
    internal static HashSet<string> ReadModelLanguages(string modelDirectory)
    {
        var languages = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(
                File.ReadAllText(Path.Combine(modelDirectory, "generation_config.json")));
            if (doc.RootElement.TryGetProperty("lang_to_id", out var map))
            {
                foreach (var entry in map.EnumerateObject())
                {
                    languages.Add(entry.Name.Trim('<', '|', '>'));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            // Without the map every language falls back to detection, which still works.
        }

        return languages;
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            if (_pipeline != 0)
            {
                OpenVinoGenAi.WhisperFree(_pipeline);
                _pipeline = 0;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
