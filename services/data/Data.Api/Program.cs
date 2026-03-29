using Airgap.Persistence;
using Confluent.Kafka;
using Data.Api.Options;
using Data.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.Configure<OutboxPublishOptions>(
    builder.Configuration.GetSection(OutboxPublishOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
    options.UseSnakeCaseNamingConvention();
});

builder.Services.AddSingleton<IConnectionStringProvider, ConfigurationConnectionStringProvider>();
builder.Services.AddSingleton<OutboxPublishState>();
builder.Services.AddSingleton<OutboxPublishControl>();

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
    return new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = opts.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true
    }).Build();
});

builder.Services.AddHostedService<OutboxPublishWorker>();

builder.Services.AddScoped<DataWriteService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

var lifetime = app.Lifetime;
var producer = app.Services.GetRequiredService<IProducer<string, string>>();
lifetime.ApplicationStopping.Register(() =>
{
    producer.Flush(TimeSpan.FromSeconds(10));
    producer.Dispose();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
