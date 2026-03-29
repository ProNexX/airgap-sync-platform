namespace Gateway.Api.Services;

public sealed class SyncServiceClient
{
    private readonly HttpClient _http;

    public SyncServiceClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("SyncService");
    }

    public async Task<object?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<object>("/sync/status", cancellationToken);
    }

    public async Task TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync("/sync/trigger", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
