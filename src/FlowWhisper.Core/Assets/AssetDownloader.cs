using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace FlowWhisper.Core.Assets;

/// <summary>
/// Downloads a <see cref="RemoteFile"/> to disk, verifying size and SHA-256.
/// Data goes to "&lt;dest&gt;.part" first and is only renamed into place once
/// verified, so an interrupted or corrupt download never looks installed.
/// An existing .part is resumed with an HTTP Range request — worth it for the
/// multi-GB model files.
/// </summary>
public sealed class AssetDownloader(HttpClient http)
{
    private const int BufferSize = 1 << 20;

    /// <param name="progress">Receives cumulative bytes written for this file.</param>
    public async Task DownloadAsync(RemoteFile file, string destinationPath, IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var partPath = destinationPath + ".part";

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var existing = await HashExistingPartAsync(partPath, file.Size, hash, cancellationToken).ConfigureAwait(false);
        progress?.Report(existing);

        if (existing < file.Size)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, file.Url);
            if (existing > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existing, null);
            }

            using var response = await http
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // A server that ignores Range sends the whole file again; start over.
            if (existing > 0 && response.StatusCode != HttpStatusCode.PartialContent)
            {
                existing = 0;
                hash.GetHashAndReset();
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var target = new FileStream(partPath, existing > 0 ? FileMode.Append : FileMode.Create,
                FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

            var buffer = new byte[BufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                hash.AppendData(buffer, 0, read);
                existing += read;
                if (existing > file.Size)
                {
                    break;
                }

                progress?.Report(existing);
            }
        }

        var actualHash = Convert.ToHexStringLower(hash.GetHashAndReset());
        if (existing != file.Size || actualHash != file.Sha256)
        {
            File.Delete(partPath);
            throw new InvalidDataException(
                $"Download of {file.RelativePath} failed verification (got {existing} bytes, SHA-256 {actualHash}).");
        }

        File.Move(partPath, destinationPath, overwrite: true);
    }

    private static async Task<long> HashExistingPartAsync(string partPath, long expectedSize, IncrementalHash hash,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(partPath))
        {
            return 0;
        }

        if (new FileInfo(partPath).Length > expectedSize)
        {
            File.Delete(partPath);
            return 0;
        }

        await using var stream = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
            useAsync: true);
        var buffer = new byte[BufferSize];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            total += read;
        }

        return total;
    }
}
