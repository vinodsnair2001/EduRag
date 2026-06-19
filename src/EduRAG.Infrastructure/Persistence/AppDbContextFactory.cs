using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EduRAG.Infrastructure.Persistence;

// Used only by EF Core design-time tools (dotnet ef migrations add/update).
// Reads appsettings.json so the connection string is not hard-coded here.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var cs = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default not found in appsettings.json.");

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs, npg => npg.UseVector())
            .Options;

        return new AppDbContext(opts);
    }
}
