namespace Data.Api.Options;

public sealed class OutboxPublishOptions
{
    public const string SectionName = "OutboxPublish";

    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 25;
}
