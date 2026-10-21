using FlowWhisper.Core.History;
using SQLite;

namespace FlowWhisper.Data;

public sealed class SqliteHistoryStore : IHistoryStore, IDisposable
{
    private readonly SQLiteAsyncConnection _connection;

    public SqliteHistoryStore(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connection = new SQLiteAsyncConnection(databasePath);
        _connection.CreateTableAsync<HistoryRow>().GetAwaiter().GetResult();
    }

    public async Task AddAsync(HistoryEntry entry)
    {
        await _connection.InsertAsync(new HistoryRow
        {
            TimestampUtcTicks = entry.TimestampUtc.Ticks,
            Text = entry.Text,
            InjectionMethod = entry.InjectionMethod,
            Cleanup = entry.Cleanup,
            Correction = entry.Correction,
        });
    }

    public async Task<List<HistoryEntry>> GetRecentAsync(int count)
    {
        var rows = await _connection.Table<HistoryRow>()
            .OrderByDescending(r => r.Id)
            .Take(count)
            .ToListAsync();

        return rows.Select(r => new HistoryEntry
        {
            Id = r.Id,
            TimestampUtc = new DateTime(r.TimestampUtcTicks, DateTimeKind.Utc),
            Text = r.Text,
            InjectionMethod = r.InjectionMethod,
            Cleanup = string.IsNullOrEmpty(r.Cleanup) ? FlowWhisper.Core.Cleanup.CleanupStatus.Off : r.Cleanup,
            Correction = string.IsNullOrEmpty(r.Correction) ? FlowWhisper.Core.Corrections.CorrectionStatus.None : r.Correction,
        }).ToList();
    }

    public async Task ClearAsync() => await _connection.DeleteAllAsync<HistoryRow>();

    public void Dispose() => _connection.CloseAsync().GetAwaiter().GetResult();
}
