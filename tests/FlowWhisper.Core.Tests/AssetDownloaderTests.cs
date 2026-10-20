using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using FlowWhisper.Core.Assets;

namespace FlowWhisper.Core.Tests;

public sealed class AssetDownloaderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "fw-dl-tests-" + Guid.NewGuid().ToString("N"));
    private readonly byte[] _content = RandomNumberGenerator.GetBytes(3_000_000);

    public AssetDownloaderTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private RemoteFile File(string? sha = null) =>
        new("x.bin", "https://example.test/x.bin", _content.Length,
            sha ?? Convert.ToHexStringLower(SHA256.HashData(_content)));

    [Fact]
    public async Task DownloadsAndVerifies()
    {
        var handler = new FakeHandler(_content, honourRange: true);
        var dest = Path.Combine(_dir, "x.bin");

        await new AssetDownloader(new HttpClient(handler)).DownloadAsync(File(), dest, null, CancellationToken.None);

        Assert.Equal(_content, await System.IO.File.ReadAllBytesAsync(dest));
        Assert.False(System.IO.File.Exists(dest + ".part"));
    }

    [Fact]
    public async Task RejectsWrongHashAndLeavesNothingBehind()
    {
        var dest = Path.Combine(_dir, "x.bin");
        var downloader = new AssetDownloader(new HttpClient(new FakeHandler(_content, honourRange: true)));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            downloader.DownloadAsync(File(new string('0', 64)), dest, null, CancellationToken.None));

        Assert.False(System.IO.File.Exists(dest));
        Assert.False(System.IO.File.Exists(dest + ".part"));
    }

    [Fact]
    public async Task ResumesFromExistingPart()
    {
        var dest = Path.Combine(_dir, "x.bin");
        await System.IO.File.WriteAllBytesAsync(dest + ".part", _content[..1_000_000]);
        var handler = new FakeHandler(_content, honourRange: true);

        await new AssetDownloader(new HttpClient(handler)).DownloadAsync(File(), dest, null, CancellationToken.None);

        Assert.Equal(1_000_000, handler.RequestedFrom);
        Assert.Equal(_content, await System.IO.File.ReadAllBytesAsync(dest));
    }

    [Fact]
    public async Task RestartsWhenServerIgnoresRange()
    {
        var dest = Path.Combine(_dir, "x.bin");
        await System.IO.File.WriteAllBytesAsync(dest + ".part", _content[..1_000_000]);

        await new AssetDownloader(new HttpClient(new FakeHandler(_content, honourRange: false)))
            .DownloadAsync(File(), dest, null, CancellationToken.None);

        Assert.Equal(_content, await System.IO.File.ReadAllBytesAsync(dest));
    }

    [Theory]
    [InlineData("pkg/runtime/bin/intel64/Release/openvino_genai_c.dll", true)]
    [InlineData("pkg/runtime/bin/intel64/Release/cache.json", true)]
    [InlineData("pkg/runtime/3rdparty/tbb/bin/tbb12.dll", true)]
    [InlineData("pkg/runtime/3rdparty/tbb/bin/tbb12_debug.dll", false)]
    [InlineData("pkg/runtime/version.txt", true)]
    [InlineData("pkg/runtime/bin/intel64/Debug/openvino.dll", false)]
    [InlineData("pkg/python/openvino/__init__.py", false)]
    [InlineData("pkg/samples/c/run.bat", false)]
    public void SelectsOnlyNeededRuntimeFiles(string entryPath, bool expected)
    {
        using var stream = new MemoryStream();
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        var entry = zip.CreateEntry(entryPath);

        Assert.Equal(expected, AssetInstaller.IsNeededRuntimeFile(entry));
    }

    private sealed class FakeHandler(byte[] content, bool honourRange) : HttpMessageHandler
    {
        public long? RequestedFrom { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedFrom = request.Headers.Range?.Ranges.First().From;
            var from = honourRange ? (int)(RequestedFrom ?? 0) : 0;
            var response = new HttpResponseMessage(from > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content, from, content.Length - from),
            };
            return Task.FromResult(response);
        }
    }
}
