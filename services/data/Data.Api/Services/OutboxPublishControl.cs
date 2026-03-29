namespace Data.Api.Services;

public sealed class OutboxPublishControl
{
    private int _wake;

    public void RequestImmediateRun() => Interlocked.Exchange(ref _wake, 1);

    public bool ConsumeWakeRequest() => Interlocked.Exchange(ref _wake, 0) == 1;
}
