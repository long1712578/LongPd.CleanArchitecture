using FluentValidation;
using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace LongPd.CleanArchitecture.Application;

/// <summary>
/// Registers all Application layer services.
/// Called from Program.cs: builder.Services.AddApplication()
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR — register all Handlers, Behaviors in order
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline order matters: Logging → Validation → Caching → Handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        // FluentValidation — auto-discover all AbstractValidator<T> in this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
