using Airgap.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data.Api;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(
            "Host=localhost;Database=app;Username=dev;Password=dev",
            npgsql => npgsql.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.GetName().Name));
        builder.UseSnakeCaseNamingConvention();
        return new AppDbContext(builder.Options);
    }
}
