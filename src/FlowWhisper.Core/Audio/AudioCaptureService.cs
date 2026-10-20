using NAudio.Wave;

namespace FlowWhisper.Core.Audio;

/// <summary>
/// Captures microphone audio via WASAPI (NAudio 3's WasapiRecorder), requesting
/// 16kHz mono PCM16 directly — the format Whisper.net expects (Milestone 3).
/// In shared mode NAudio/WASAPI transparently resamples/downmixes from the
/// device's native mix format to the requested format, so no separate
/// post-processing step is needed.
/// </summary>
public sealed class AudioCaptureService : IAudioCaptureService
{
    private static readonly WaveFormat TargetFormat = new(16000, 16, 1);

    private readonly string _recordingsDir;
    private readonly ManualResetEventSlim _stoppedSignal = new(initialState: false);

    private WasapiRecorder? _recorder;
    private WaveFileWriter? _writer;
    private string? _path;

    // In-memory copy of the PCM recorded so far, for live-preview transcription
    // while the key is still held. Written on the capture thread, read from the preview loop.
    private readonly MemoryStream _pcm = new();
    private readonly object _pcmLock = new();

    public event Action<float>? VolumeChanged;

    public string? PreferredDeviceId { get; set; }

    public float PeakLevel { get; private set; }

    public string DeviceName { get; private set; } = string.Empty;

    public AudioCaptureService()
    {
        _recordingsDir = AppPaths.Recordings;
        Directory.CreateDirectory(_recordingsDir);
    }

    public void StartRecording()
    {
        if (_recorder is not null)
        {
            throw new InvalidOperationException("Recording is already in progress.");
        }

        lock (_pcmLock)
        {
            _pcm.SetLength(0);
        }

        // 50 ms buffers give ~20 level updates per second — smooth enough for the waveform.
        var builder = new WasapiRecorderBuilder().WithFormat(TargetFormat).WithBufferLength(50);
        using (var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator())
        {
            // Resolve the device explicitly (rather than letting the builder pick the
            // default) so its name can be shown when it turns out to be silent.
            var device = string.IsNullOrEmpty(PreferredDeviceId)
                ? enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.Role.Console)
                : enumerator.GetDevice(PreferredDeviceId);
            DeviceName = device.FriendlyName;
            builder = builder.WithDevice(device);
        }

        PeakLevel = 0;

        // WasapiRecorder raises RecordingStopped on the SynchronizationContext it was built
        // on. Built on the UI thread (where the hotkey fires), StopRecording — also on the UI
        // thread — would wait for an event that can only arrive once it returns, stalling
        // every release for the full 2 s timeout. Building without a context avoids that.
        var uiContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            _recorder = builder.Build();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(uiContext);
        }
        _path = Path.Combine(_recordingsDir, $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");
        _writer = new WaveFileWriter(_path, _recorder.WaveFormat);

        _recorder.DataAvailable += OnDataAvailable;
        _recorder.RecordingStopped += OnRecordingStopped;

        _stoppedSignal.Reset();
        _recorder.StartRecording();
    }

    private void OnDataAvailable(
        ReadOnlySpan<byte> buffer,
        NAudio.CoreAudioApi.AudioClientBufferFlags flags,
        long devicePosition,
        long qpcPosition)
    {
        _writer?.Write(buffer);
        lock (_pcmLock)
        {
            _pcm.Write(buffer);
        }

        var level = ComputePeakLevel(buffer);
        PeakLevel = Math.Max(PeakLevel, level);
        VolumeChanged?.Invoke(level);
    }

    public TimeSpan RecordedDuration
    {
        get
        {
            lock (_pcmLock)
            {
                return TimeSpan.FromSeconds((double)_pcm.Length / TargetFormat.AverageBytesPerSecond);
            }
        }
    }

    public Stream CreateSnapshot()
    {
        byte[] pcm;
        lock (_pcmLock)
        {
            pcm = _pcm.ToArray();
        }

        return WavFile.Wrap(pcm, TargetFormat.SampleRate, TargetFormat.Channels, TargetFormat.BitsPerSample);
    }

    private static float ComputePeakLevel(ReadOnlySpan<byte> buffer)
    {
        var max = 0f;
        for (var i = 0; i + 2 <= buffer.Length; i += 2)
        {
            var sample = MathF.Abs(BitConverter.ToInt16(buffer.Slice(i, 2)) / 32768f);
            if (sample > max)
            {
                max = sample;
            }
        }

        return Math.Clamp(max, 0f, 1f);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e) => _stoppedSignal.Set();

    public string StopRecording()
    {
        if (_recorder is null || _writer is null || _path is null)
        {
            throw new InvalidOperationException("No recording in progress.");
        }

        _recorder.StopRecording();

        // StopRecording() signals the capture loop but may return before it
        // has fully drained; wait for RecordingStopped so no DataAvailable
        // calls race the writer's disposal below.
        _stoppedSignal.Wait(TimeSpan.FromSeconds(2));

        _writer.Dispose();
        _recorder.DataAvailable -= OnDataAvailable;
        _recorder.RecordingStopped -= OnRecordingStopped;
        _recorder.Dispose();

        var path = _path;
        _recorder = null;
        _writer = null;
        _path = null;

        return path;
    }

    public void Dispose()
    {
        _recorder?.Dispose();
        _writer?.Dispose();
        _stoppedSignal.Dispose();
    }
}
