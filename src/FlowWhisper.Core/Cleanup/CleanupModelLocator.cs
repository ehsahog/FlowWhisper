namespace FlowWhisper.Core.Cleanup;

public static class CleanupModelLocator
{
    /// <summary>Intel's NPU/GPU-ready OpenVINO export (OpenVINO/Phi-3.5-mini-instruct-int4-cw-ov).</summary>
    public static string GetModelDirectory() => Path.Combine(AppPaths.Models, "phi35-mini-npu");

    /// <summary>OpenVINO GenAI 2026.4 runtime DLLs, including openvino_genai_c.dll.</summary>
    public static string GetRuntimeDirectory() => Path.Combine(AppPaths.Runtime, "openvino");

    /// <summary>Compiled-model cache; makes later loads ~4s on GPU instead of ~50s.</summary>
    public static string GetCacheDirectory() => Path.Combine(AppPaths.Cache, "ov");
}
