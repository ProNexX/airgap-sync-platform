namespace Gateway.Api.Services;

public class DataServiceClient
{
    private readonly HttpClient _http;

    public DataServiceClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("DataService");
    }

    public async Task<object> CreateAsync(object request)
    {
        var response = await _http.PostAsJsonAsync("/data", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object> GetAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<object>($"/data/{id}");
    }
}
