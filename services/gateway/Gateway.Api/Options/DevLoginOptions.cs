namespace Gateway.Api.Options;

public sealed class DevLoginOptions
{
    public const string SectionName = "DevLogin";

    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}
