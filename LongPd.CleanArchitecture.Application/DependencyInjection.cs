using FluentValidation;
using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace LongPd.CleanArchitecture.Application;

/// <summary>
/// Registers all Application layer services.
/// Called from Program.cs: builder.Services.AddApplication()
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        
        var assemblies = new List<Assembly> { assembly };
        assemblies.AddRange(additionalAssemblies);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies([.. assemblies]);

            // Pipeline order matters: Idempotency → Logging → Validation → Caching → CacheInvalidation → Handler
            cfg.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        // FluentValidation — auto-discover all AbstractValidator<T> in this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
