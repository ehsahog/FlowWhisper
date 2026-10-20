using FlowWhisper.Core.Assets;

namespace FlowWhisper.Core.Transcription;

public static class WhisperModelLocator
{
    public static string GetModelDirectory(string model) =>
        Path.Combine(AppPaths.Models, AssetManifest.GetWhisperModel(model).Directory);
}
