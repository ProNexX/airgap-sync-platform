namespace Sync.Api.Services;

public sealed class ConsumerState
{
    private readonly object _gate = new();

    public long TotalMessages { get; private set; }
    public DateTimeOffset? LastConsumedAt { get; private set; }
    public string? LastTopic { get; private set; }
    public int? LastPartition { get; private set; }
    public long? LastOffset { get; private set; }
    public string? LastKey { get; private set; }
    public string? LastError { get; private set; }

    public void RecordConsumed(
        string topic,
        int partition,
        long offset,
        string? key)
    {
        lock (_gate)
        {
            checked { TotalMessages++; }
            LastConsumedAt = DateTimeOffset.UtcNow;
            LastTopic = topic;
            LastPartition = partition;
            LastOffset = offset;
            LastKey = key;
            LastError = null;
        }
    }

    public void RecordError(string message)
    {
        lock (_gate)
        {
            LastError = message;
        }
    }

    public object Snapshot()
    {
        lock (_gate)
        {
            return new
            {
                role = "kafka-consumer",
                totalMessages = TotalMessages,
                lastConsumedAt = LastConsumedAt,
                lastTopic = LastTopic,
                lastPartition = LastPartition,
                lastOffset = LastOffset,
                lastKey = LastKey,
                lastError = LastError
            };
        }
    }
}
