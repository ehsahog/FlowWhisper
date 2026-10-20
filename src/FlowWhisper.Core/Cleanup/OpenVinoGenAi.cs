using System.Reflection;
using System.Runtime.InteropServices;

namespace FlowWhisper.Core.Cleanup;

/// <summary>
/// P/Invoke surface for the OpenVINO GenAI C API (openvino_genai_c.dll, 2026.4):
/// the LLM pipeline for cleanup and the Whisper pipeline for transcription.
/// All strings crossing the boundary are UTF-8.
/// </summary>
internal static partial class OpenVinoGenAi
{
    private const string Lib = "openvino_genai_c.dll";

    private static readonly object ResolverLock = new();
    private static bool s_resolverSet;

    /// <summary>
    /// Points DllImport at the downloaded runtime folder. Loading by absolute path makes
    /// Windows resolve the DLL's own dependencies (openvino.dll, TBB, device plugins) from
    /// that same folder. Safe to call repeatedly; the first runtime directory wins.
    /// </summary>
    public static void Initialize(string runtimeDirectory)
    {
        lock (ResolverLock)
        {
            if (s_resolverSet)
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (name, _, _) =>
                name == Lib ? NativeLibrary.Load(Path.Combine(runtimeDirectory, name)) : 0);
            s_resolverSet = true;
        }
    }

    // ---- Whisper pipeline ----

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_pipeline_create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WhisperCreate(string modelsPath, string device, nuint propertyArgsSize, out nint pipeline,
        string key1, string value1);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_pipeline_free")]
    public static partial void WhisperFree(nint pipeline);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_pipeline_get_generation_config")]
    public static partial int WhisperGetConfig(nint pipeline, out nint config);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_generation_config_free")]
    public static partial void WhisperConfigFree(nint config);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_generation_config_set_language", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WhisperConfigSetLanguage(nint config, string language);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_generation_config_set_task", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WhisperConfigSetTask(nint config, string task);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_pipeline_generate")]
    public static unsafe partial int WhisperGenerate(nint pipeline, float* rawSpeech, nuint rawSpeechSize, nint config,
        out nint results);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_decoded_results_get_string")]
    private static partial int WhisperResultsGetString(nint results, nint output, ref nuint outputSize);

    [LibraryImport(Lib, EntryPoint = "ov_genai_whisper_decoded_results_free")]
    public static partial void WhisperResultsFree(nint results);

    public static string ReadWhisperResultString(nint results) =>
        ReadUtf8((nint buffer, ref nuint size) => WhisperResultsGetString(results, buffer, ref size),
            "whisper_decoded_results_get_string");

    // ---- LLM pipeline ----

    // ov_genai_llm_pipeline_create is variadic (key/value property pairs). On
    // Windows x64, variadic pointer arguments are passed exactly like fixed ones,
    // so fixed-arity overloads are a safe way to call it.
    [LibraryImport(Lib, EntryPoint = "ov_genai_llm_pipeline_create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int PipelineCreate(string modelsPath, string device, nuint propertyArgsSize, out nint pipe,
        string key1, string value1);

    [LibraryImport(Lib, EntryPoint = "ov_genai_llm_pipeline_create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int PipelineCreate(string modelsPath, string device, nuint propertyArgsSize, out nint pipe,
        string key1, string value1, string key2, string value2, string key3, string value3);

    [LibraryImport(Lib, EntryPoint = "ov_genai_llm_pipeline_free")]
    public static partial void PipelineFree(nint pipe);

    [LibraryImport(Lib, EntryPoint = "ov_genai_llm_pipeline_generate_with_history")]
    public static partial int GenerateWithHistory(nint pipe, nint history, nint config, nint streamer, out nint results);

    [LibraryImport(Lib, EntryPoint = "ov_genai_generation_config_create")]
    public static partial int ConfigCreate(out nint config);

    [LibraryImport(Lib, EntryPoint = "ov_genai_generation_config_free")]
    public static partial void ConfigFree(nint config);

    [LibraryImport(Lib, EntryPoint = "ov_genai_generation_config_set_max_new_tokens")]
    public static partial int ConfigSetMaxNewTokens(nint config, nuint value);

    [LibraryImport(Lib, EntryPoint = "ov_genai_generation_config_set_do_sample")]
    public static partial int ConfigSetDoSample(nint config, [MarshalAs(UnmanagedType.U1)] bool value);

    [LibraryImport(Lib, EntryPoint = "ov_genai_generation_config_set_stop_strings")]
    private static partial int ConfigSetStopStrings(nint config, nint strings, nuint count);

    public static void SetStopStrings(nint config, params string[] stops)
    {
        var pointers = stops.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
        var array = Marshal.AllocHGlobal(nint.Size * pointers.Length);
        try
        {
            Marshal.Copy(pointers, 0, array, pointers.Length);
            Check(ConfigSetStopStrings(config, array, (nuint)pointers.Length), "set_stop_strings");
        }
        finally
        {
            Marshal.FreeHGlobal(array);
            foreach (var p in pointers)
            {
                Marshal.FreeCoTaskMem(p);
            }
        }
    }

    [LibraryImport(Lib, EntryPoint = "ov_genai_json_container_create_from_json_string", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int JsonCreateFromString(out nint container, string json);

    [LibraryImport(Lib, EntryPoint = "ov_genai_json_container_free")]
    public static partial void JsonFree(nint container);

    [LibraryImport(Lib, EntryPoint = "ov_genai_chat_history_create_from_json_container")]
    public static partial int HistoryCreateFromJson(out nint history, nint messages);

    [LibraryImport(Lib, EntryPoint = "ov_genai_chat_history_free")]
    public static partial void HistoryFree(nint history);

    [LibraryImport(Lib, EntryPoint = "ov_genai_decoded_results_get_string")]
    public static partial int ResultsGetString(nint results, nint output, ref nuint outputSize);

    [LibraryImport(Lib, EntryPoint = "ov_genai_decoded_results_free")]
    public static partial void ResultsFree(nint results);

    public static void Check(int status, string call)
    {
        if (status != 0)
        {
            throw new InvalidOperationException($"OpenVINO GenAI call {call} failed with status {status}.");
        }
    }

    public static string ReadResultString(nint results) =>
        ReadUtf8((nint buffer, ref nuint size) => ResultsGetString(results, buffer, ref size),
            "decoded_results_get_string");

    private delegate int StringGetter(nint buffer, ref nuint size);

    /// <summary>The C API's two-call pattern: ask for the size, then fill a buffer of that size.</summary>
    private static string ReadUtf8(StringGetter get, string call)
    {
        nuint size = 0;
        Check(get(0, ref size), call + "(size)");

        var buffer = Marshal.AllocHGlobal((nint)size);
        try
        {
            Check(get(buffer, ref size), call);
            return Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
