using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Infrastructure.Caching;
using LongPd.CleanArchitecture.Infrastructure.Data;
using LongPd.CleanArchitecture.Infrastructure.Persistence;
using LongPd.CleanArchitecture.Infrastructure.Persistence.Repositories;
using LongPd.CleanArchitecture.Infrastructure.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LongPd.CleanArchitecture.Infrastructure;

/// <summary>
/// Infrastructure DI registration.
/// Called from Program.cs: builder.Services.AddInfrastructure(builder.Configuration)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IDbConnectionFactory>(_ =>
            new NpgsqlConnectionFactory(
                configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default is required.")));

        services.AddMemoryCache();

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "CleanArchitectureApp:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // Register the HybridCacheService
        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }
}
