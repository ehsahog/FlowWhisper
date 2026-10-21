using FlowWhisper.Core.Injection;

namespace FlowWhisper.Core.Tests;

public class RemoteDesktopClientsTests
{
    [Theory]
    [InlineData("vmware-remotemks", true)]
    [InlineData("VMWARE-VIEW", true)]
    [InlineData("wfica32", true)]
    [InlineData("mstsc", true)]
    [InlineData("msrdc", true)]
    [InlineData("notepad", false)]
    [InlineData("Claude", false)]
    public void RecognizesRemoteDesktopClients(string processName, bool expected) =>
        Assert.Equal(expected, RemoteDesktopClients.IsRemoteClient(processName));
}
