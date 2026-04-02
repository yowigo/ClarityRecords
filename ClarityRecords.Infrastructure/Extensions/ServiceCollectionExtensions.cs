using ClarityRecords.Domain.Services;
using ClarityRecords.Infrastructure.Data;
using ClarityRecords.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClarityRecords.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

        return services;
    }
}
