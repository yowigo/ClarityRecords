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

        // 为 Identity 提供 Scoped AppDbContext（从工厂派生，避免生命周期冲突）
        services.AddScoped<AppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

        return services;
    }
}
