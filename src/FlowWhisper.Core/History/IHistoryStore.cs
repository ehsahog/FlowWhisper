namespace FlowWhisper.Core.History;

public interface IHistoryStore
{
    Task AddAsync(HistoryEntry entry);

    /// <summary>Most recent entries first.</summary>
    Task<List<HistoryEntry>> GetRecentAsync(int count);

    Task ClearAsync();
}
