namespace Gateway.Api.Services;

public sealed class TelemetryServiceClient
{
    private readonly HttpClient _http;

    public TelemetryServiceClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("TelemetryService");
    }

    public async Task<object?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<object>("/telemetry/status", cancellationToken);
    }

    public async Task TriggerAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync("/telemetry/trigger", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
