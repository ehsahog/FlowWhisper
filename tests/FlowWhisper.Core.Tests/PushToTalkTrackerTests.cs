using FlowWhisper.Core.Hotkey;

namespace FlowWhisper.Core.Tests;

public class PushToTalkTrackerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PressAndReleaseStartAndStopOnce()
    {
        var tracker = new PushToTalkTracker();
        Assert.True(tracker.KeyDown(T0));
        Assert.False(tracker.KeyDown(T0.AddMilliseconds(500)));
        Assert.True(tracker.KeyUp());
        Assert.False(tracker.KeyUp());
    }

    [Fact]
    public void HeldKeyThatKeepsRepeatingStaysDown()
    {
        var tracker = new PushToTalkTracker();
        tracker.KeyDown(T0);
        for (var ms = 500; ms <= 60_000; ms += 33)
        {
            tracker.KeyDown(T0.AddMilliseconds(ms));
            Assert.False(tracker.Tick(T0.AddMilliseconds(ms + 10)));
        }
    }

    [Fact]
    public void StopsWhenRepeatsStopWithoutKeyUp()
    {
        var tracker = new PushToTalkTracker();
        tracker.KeyDown(T0);
        tracker.KeyDown(T0.AddMilliseconds(500));
        tracker.KeyDown(T0.AddMilliseconds(533));

        Assert.False(tracker.Tick(T0.AddMilliseconds(1500)));
        Assert.True(tracker.Tick(T0.AddMilliseconds(2100)));
        Assert.False(tracker.IsDown);
        Assert.False(tracker.KeyUp()); // a late key-up doesn't stop it twice
    }

    [Fact]
    public void WithoutRepeatsOnlyTheMaxHoldStopsIt()
    {
        var tracker = new PushToTalkTracker();
        tracker.KeyDown(T0);
        Assert.False(tracker.Tick(T0.AddSeconds(30)));
        Assert.True(tracker.Tick(T0 + PushToTalkTracker.MaxHold + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void NewPressAfterMissedReleaseStartsAgain()
    {
        var tracker = new PushToTalkTracker();
        tracker.KeyDown(T0);
        tracker.KeyDown(T0.AddMilliseconds(500));
        Assert.True(tracker.Tick(T0.AddSeconds(3)));
        Assert.True(tracker.KeyDown(T0.AddSeconds(5)));
    }
}
