using FlowWhisper.Core.Transcription;

namespace FlowWhisper.Core.Tests;

public class ModelLanguagesTests
{
    [Fact]
    public void ReadsLanguageCodesFromGenerationConfig()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, "generation_config.json"),
                """{ "lang_to_id": { "<|en|>": 50259, "<|sv|>": 50273, "<|yue|>": 50358 } }""");

            var languages = OpenVinoWhisperService.ReadModelLanguages(dir);

            Assert.Equal(["en", "sv", "yue"], languages.Order());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void MissingConfigMeansNoKnownLanguages()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.Empty(OpenVinoWhisperService.ReadModelLanguages(dir));
        }
        finally
        {
            Directory.Delete(dir);
        }
    }
}
