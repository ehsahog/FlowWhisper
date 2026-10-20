using FlowWhisper.Core.Hotkey;

namespace FlowWhisper.Core.Tests;

public class VirtualKeyMapTests
{
    [Theory]
    [InlineData("RightShift", 0xA1)]
    [InlineData("LeftShift", 0xA0)]
    [InlineData("rightshift", 0xA1)]
    public void Resolve_KnownName_ReturnsExpectedVirtualKeyCode(string name, uint expected)
    {
        var vk = VirtualKeyMap.Resolve(name);
        Assert.Equal(expected, vk);
    }

    [Fact]
    public void Resolve_UnknownName_Throws()
    {
        Assert.Throws<ArgumentException>(() => VirtualKeyMap.Resolve("NotAKey"));
    }
}
