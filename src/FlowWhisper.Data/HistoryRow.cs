using SQLite;

namespace FlowWhisper.Data;

/// <summary>
/// The SQLite-mapped row shape. Kept separate from FlowWhisper.Core.History.HistoryEntry
/// so sqlite-net-pcl attributes don't leak into Core, which stays storage-agnostic.
/// </summary>
internal sealed class HistoryRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public long TimestampUtcTicks { get; set; }

    public string Text { get; set; } = string.Empty;

    public string InjectionMethod { get; set; } = string.Empty;

    // Added after the first schema; sqlite-net's CreateTable adds the missing column on existing databases.
    public string Cleanup { get; set; } = string.Empty;

    public string Correction { get; set; } = string.Empty;
}
