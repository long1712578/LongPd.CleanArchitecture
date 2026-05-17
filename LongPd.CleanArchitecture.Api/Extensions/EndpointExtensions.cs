using LongPd.CleanArchitecture.Api.Endpoints;

namespace LongPd.CleanArchitecture.Api.Extensions;

public static class EndpointExtensions
{
    /// <summary>
    /// Discovers and registers all endpoint definitions in the current assembly.
    /// </summary>
    public static IServiceCollection AddEndpointDefinitions(this IServiceCollection services)
    {
        var endpointDefinitions = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(IEndpointDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>()
            .ToList();

        foreach (var endpointDefinition in endpointDefinitions)
        {
            services.AddSingleton(endpointDefinition);
        }

        return services;
    }

    /// <summary>
    /// Maps all registered endpoint definitions in the request pipeline.
    /// </summary>
    public static IApplicationBuilder MapEndpointDefinitions(this WebApplication app)
    {
        var endpointDefinitions = app.Services.GetServices<IEndpointDefinition>();

        foreach (var endpointDefinition in endpointDefinitions)
        {
            endpointDefinition.RegisterEndpoints(app);
        }

        return app;
    }
}
