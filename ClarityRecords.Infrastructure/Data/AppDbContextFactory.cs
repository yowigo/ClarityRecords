using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClarityRecords.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations. Not used at runtime.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=clarityrecords;Username=root;Password=123456")
            .Options;
        return new AppDbContext(options);
    }
}
