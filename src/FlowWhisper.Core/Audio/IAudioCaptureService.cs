namespace FlowWhisper.Core.Audio;

public interface IAudioCaptureService : IDisposable
{
    /// <summary>Raised roughly on each captured buffer with a 0..1 volume level, for the FlowBar meter.</summary>
    event Action<float>? VolumeChanged;

    /// <summary>MMDevice id to record from, or null for the system default microphone.</summary>
    string? PreferredDeviceId { get; set; }

    void StartRecording();

    /// <summary>Highest 0..1 peak seen in the current/last recording. ~0 means the mic delivered digital silence.</summary>
    float PeakLevel { get; }

    /// <summary>Friendly name of the device used for the current/last recording.</summary>
    string DeviceName { get; }

    /// <summary>How much audio the current recording holds so far.</summary>
    TimeSpan RecordedDuration { get; }

    /// <summary>A WAV stream of everything recorded so far, while recording continues.</summary>
    Stream CreateSnapshot();

    /// <summary>Stops recording and returns the path to the captured 16kHz mono WAV file.</summary>
    string StopRecording();
}
