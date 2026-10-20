using NAudio.CoreAudioApi;

namespace FlowWhisper.Core.Audio;

public static class AudioDeviceProvider
{
    public static List<AudioDeviceInfo> GetCaptureDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => new AudioDeviceInfo(d.ID, d.FriendlyName))
            .ToList();
    }
}
