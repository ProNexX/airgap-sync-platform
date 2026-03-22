var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP clients for downstream services
builder.Services.AddHttpClient("DataService", client =>
{
    client.BaseAddress = new Uri("http://data-service:8080");
});

builder.Services.AddHttpClient("SyncService", client =>
{
    client.BaseAddress = new Uri("http://sync-service:8080");
});

builder.Services.AddScoped<DataServiceClient>();
builder.Services.AddScoped<SyncServiceClient>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();