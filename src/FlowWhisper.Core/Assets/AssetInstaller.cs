using System.IO.Compression;
using FlowWhisper.Core.Cleanup;
using FlowWhisper.Core.Transcription;

namespace FlowWhisper.Core.Assets;

public readonly record struct InstallProgress(long BytesDone, long BytesTotal);

/// <summary>
/// Knows what "installed" means for each downloadable asset and installs what's missing.
/// Both speech recognition and cleanup run on the shared OpenVINO runtime, which is
/// downloaded alongside whichever model needs it first.
/// </summary>
public sealed class AssetInstaller(AssetDownloader downloader)
{
    /// <summary>Bytes still to download for this Whisper model (including the runtime if missing).</summary>
    public long WhisperDownloadSize(string model) =>
        MissingBytes(AssetManifest.GetWhisperModel(model).Files, WhisperModelLocator.GetModelDirectory(model));

    /// <summary>Bytes still to download for cleanup (including the runtime if missing).</summary>
    public long CleanupDownloadSize =>
        MissingBytes(AssetManifest.CleanupModel, CleanupModelLocator.GetModelDirectory());

    public bool IsWhisperInstalled(string model) =>
        AreFilesPresent(AssetManifest.GetWhisperModel(model).Files, WhisperModelLocator.GetModelDirectory(model))
        && IsRuntimeInstalled();

    public Task InstallWhisperAsync(string model, IProgress<InstallProgress>? progress,
        CancellationToken cancellationToken) =>
        InstallWithRuntimeAsync(AssetManifest.GetWhisperModel(model).Files, WhisperModelLocator.GetModelDirectory(model),
            progress, cancellationToken);

    public bool IsCleanupInstalled() =>
        AreFilesPresent(AssetManifest.CleanupModel, CleanupModelLocator.GetModelDirectory()) && IsRuntimeInstalled();

    public Task InstallCleanupAsync(IProgress<InstallProgress>? progress, CancellationToken cancellationToken) =>
        InstallWithRuntimeAsync(AssetManifest.CleanupModel, CleanupModelLocator.GetModelDirectory(),
            progress, cancellationToken);

    private async Task InstallWithRuntimeAsync(IReadOnlyList<RemoteFile> files, string directory,
        IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        var total = MissingBytes(files, directory);
        long done = 0;

        foreach (var file in files)
        {
            var path = Path.Combine(directory, file.RelativePath);
            if (IsPresent(path, file))
            {
                continue;
            }

            await downloader.DownloadAsync(file, path, Relay(progress, done, total), cancellationToken)
                .ConfigureAwait(false);
            done += file.Size;
            progress?.Report(new InstallProgress(done, total));
        }

        if (!IsRuntimeInstalled())
        {
            await InstallRuntimeAsync(Relay(progress, done, total), cancellationToken).ConfigureAwait(false);
        }

        progress?.Report(new InstallProgress(total, total));
    }

    private static long MissingBytes(IReadOnlyList<RemoteFile> files, string directory) =>
        files.Where(f => !IsPresent(Path.Combine(directory, f.RelativePath), f)).Sum(f => f.Size)
        + (IsRuntimeInstalled() ? 0 : AssetManifest.OpenVinoRuntimeArchive.Size);

    private static bool AreFilesPresent(IReadOnlyList<RemoteFile> files, string directory) =>
        files.All(f => IsPresent(Path.Combine(directory, f.RelativePath), f));

    private static bool IsRuntimeInstalled()
    {
        var dir = CleanupModelLocator.GetRuntimeDirectory();
        var versionFile = Path.Combine(dir, "version.txt");
        return File.Exists(Path.Combine(dir, "openvino_genai_c.dll"))
            && File.Exists(versionFile)
            && File.ReadAllText(versionFile).StartsWith(AssetManifest.OpenVinoRuntimeVersion, StringComparison.Ordinal);
    }

    private async Task InstallRuntimeAsync(IProgress<long>? progress, CancellationToken cancellationToken)
    {
        var runtimeDir = CleanupModelLocator.GetRuntimeDirectory();
        var parent = Path.GetDirectoryName(runtimeDir)!;
        var archive = AssetManifest.OpenVinoRuntimeArchive;
        var zipPath = Path.Combine(parent, "download", archive.RelativePath);

        await downloader.DownloadAsync(archive, zipPath, progress, cancellationToken).ConfigureAwait(false);

        // Extract to a staging folder and swap it in at the end, so a crash halfway
        // through never leaves a runtime folder that looks complete.
        var staging = runtimeDir + ".staging";
        if (Directory.Exists(staging))
        {
            Directory.Delete(staging, recursive: true);
        }

        Directory.CreateDirectory(staging);
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in zip.Entries.Where(IsNeededRuntimeFile))
            {
                // Entry.Name only (never the archive's path), so a crafted entry can't
                // write outside the staging folder.
                entry.ExtractToFile(Path.Combine(staging, entry.Name), overwrite: true);
            }
        }

        if (Directory.Exists(runtimeDir))
        {
            Directory.Delete(runtimeDir, recursive: true);
        }

        Directory.Move(staging, runtimeDir);
        File.Delete(zipPath);
    }

    internal static bool IsNeededRuntimeFile(ZipArchiveEntry entry)
    {
        var path = entry.FullName.Replace('\\', '/');
        if (entry.Name.Length == 0)
        {
            return false;
        }

        return path.Contains("/runtime/bin/intel64/Release/", StringComparison.Ordinal)
            || (path.Contains("/runtime/3rdparty/tbb/bin/", StringComparison.Ordinal)
                && entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && !entry.Name.Contains("_debug", StringComparison.OrdinalIgnoreCase))
            || path.EndsWith("/runtime/version.txt", StringComparison.Ordinal);
    }

    private static bool IsPresent(string path, RemoteFile file) =>
        File.Exists(path) && new FileInfo(path).Length == file.Size;

    private static IProgress<long>? Relay(IProgress<InstallProgress>? progress, long offset, long total) =>
        progress is null ? null : new SyncProgress<long>(bytes => progress.Report(new InstallProgress(offset + bytes, total)));

    /// <summary>Reports synchronously on the calling thread, unlike <see cref="Progress{T}"/>.</summary>
    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
