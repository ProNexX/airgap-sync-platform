namespace Data.Api.Services;

public interface IConnectionStringProvider
{
    string GetConnectionString();
}

public sealed class ConfigurationConnectionStringProvider : IConnectionStringProvider
{
    private readonly string _connectionString;

    public ConfigurationConnectionStringProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public string GetConnectionString() => _connectionString;
}
