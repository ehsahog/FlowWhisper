using FlowWhisper.Core.Corrections;

namespace FlowWhisper.Core.Tests;

public class SelfCorrectorTests
{
    private const string TimeCorrection = "Vi ses kl 12, nej förresten kl 13.";
    private const string LooseCorrection = "Vi tar bussen i morgon, fast vänta, tåget går snabbare.";
    private const string Plain = "Vi har tre katter och en hund.";

    private static SelfCorrector.ModelCorrection Model(string answer, bool accepted = true) =>
        (_, _) => Task.FromResult((answer, accepted, answer));

    private static SelfCorrector.ModelCorrection CountingModel(List<string> calls) =>
        (text, _) => { calls.Add(text); return Task.FromResult((text, true, text)); };

    [Fact]
    public async Task OffLeavesTextAlone() =>
        Assert.Equal((TimeCorrection, CorrectionStatus.None), await SelfCorrector.ApplyAsync(TimeCorrection, CorrectionMode.Off, Model("x")));

    [Fact]
    public async Task RulesNeverCallTheModel()
    {
        var calls = new List<string>();
        Assert.Equal(("Vi ses kl 13.", CorrectionStatus.Rules), await SelfCorrector.ApplyAsync(TimeCorrection, CorrectionMode.Rules, CountingModel(calls)));
        Assert.Empty(calls);
    }

    [Fact]
    public async Task HybridSkipsTheModelWhenRulesSucceedOrNoCue()
    {
        var calls = new List<string>();
        Assert.Equal(("Vi ses kl 13.", CorrectionStatus.Rules), await SelfCorrector.ApplyAsync(TimeCorrection, CorrectionMode.Hybrid, CountingModel(calls)));
        Assert.Equal((Plain, CorrectionStatus.None), await SelfCorrector.ApplyAsync(Plain, CorrectionMode.Hybrid, CountingModel(calls)));
        Assert.Empty(calls);
    }

    [Fact]
    public async Task HybridAsksTheModelForCorrectionsRulesCantHandle() =>
        Assert.Equal(("Vi tar tåget i morgon.", CorrectionStatus.Ai),
            await SelfCorrector.ApplyAsync(LooseCorrection, CorrectionMode.Hybrid, Model("Vi tar tåget i morgon.")));

    [Fact]
    public async Task AiModeAsksTheModelForEverything()
    {
        var calls = new List<string>();
        await SelfCorrector.ApplyAsync(Plain, CorrectionMode.Ai, CountingModel(calls));
        Assert.Equal([Plain], calls);
    }

    [Fact]
    public async Task RejectedOrRepunctuatedAnswersKeepTheDictation()
    {
        Assert.Equal((LooseCorrection, CorrectionStatus.AiRejected),
            await SelfCorrector.ApplyAsync(LooseCorrection, CorrectionMode.Ai, Model("Något annat.", accepted: false)));
        Assert.Equal((Plain, CorrectionStatus.None),
            await SelfCorrector.ApplyAsync(Plain, CorrectionMode.Ai, Model("vi har tre katter och en hund")));
    }

    [Fact]
    public async Task AiModeNeverUsesTheRules()
    {
        // The rules would turn this into "Vi ses kl 13.", but AI mode must show only the model's work.
        Assert.Equal((TimeCorrection, CorrectionStatus.AiRejected),
            await SelfCorrector.ApplyAsync(TimeCorrection, CorrectionMode.Ai, Model("Vi ses kl 12.", accepted: false)));
        Assert.Equal((TimeCorrection, CorrectionStatus.AiNotReady), await SelfCorrector.ApplyAsync(TimeCorrection, CorrectionMode.Ai, null));
        Assert.Equal((TimeCorrection, CorrectionStatus.None),
            await SelfCorrector.ApplyAsync(TimeCorrection, CorrectionMode.Ai, Model("vi ses kl 12 nej förresten kl 13")));
    }

    [Fact]
    public async Task MissingSlowOrFailingModelKeepsTheDictation()
    {
        Assert.Equal((LooseCorrection, CorrectionStatus.AiNotReady), await SelfCorrector.ApplyAsync(LooseCorrection, CorrectionMode.Hybrid, null));

        SelfCorrector.ModelCorrection slow = async (t, ct) => { await Task.Delay(5000, ct); return (t, true, t); };
        Assert.Equal((LooseCorrection, CorrectionStatus.AiTimeout),
            await SelfCorrector.ApplyAsync(LooseCorrection, CorrectionMode.Hybrid, slow, TimeSpan.FromMilliseconds(50)));

        SelfCorrector.ModelCorrection failing = (_, _) => throw new InvalidOperationException("boom");
        Assert.Equal((LooseCorrection, CorrectionStatus.AiError), await SelfCorrector.ApplyAsync(LooseCorrection, CorrectionMode.Hybrid, failing));
    }
}
