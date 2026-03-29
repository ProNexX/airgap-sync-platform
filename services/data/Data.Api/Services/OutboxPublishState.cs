namespace Data.Api.Services;

public sealed class OutboxPublishState
{
    private readonly object _gate = new();

    public DateTimeOffset? LastPublishCompletedAt { get; private set; }
    public int LastMessagesPublished { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? LastKafkaDeliveryAt { get; private set; }

    public void RecordBatch(int count, DateTimeOffset at, DateTimeOffset? kafkaAt, string? error)
    {
        lock (_gate)
        {
            LastMessagesPublished = count;
            LastPublishCompletedAt = at;
            LastKafkaDeliveryAt = kafkaAt;
            LastError = error;
        }
    }

    public object Snapshot()
    {
        lock (_gate)
        {
            return new
            {
                lastPublishCompletedAt = LastPublishCompletedAt,
                lastMessagesPublished = LastMessagesPublished,
                lastKafkaDeliveryAt = LastKafkaDeliveryAt,
                lastError = LastError
            };
        }
    }
}
