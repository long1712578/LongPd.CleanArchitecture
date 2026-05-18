using LongPd.CleanArchitecture.Api.Extensions;
using LongPd.CleanArchitecture.Api.Grpc;
using LongPd.CleanArchitecture.Api.Middleware;
using LongPd.CleanArchitecture.Api.Services;
using LongPd.CleanArchitecture.Application;
using LongPd.CleanArchitecture.Application.Abstractions.Notifications;
using LongPd.CleanArchitecture.Application.Abstractions.Services;
using LongPd.CleanArchitecture.Infrastructure;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add OpenTelemetry for Jaeger (Tracing) and Prometheus (Metrics)
builder.AddOpenTelemetryObservability();

// ─── API Presentation Layer Concerns ───────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// CORS for gRPC-Web and React clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding", "X-Correlation-Id");
    });
});

// Configure Rate Limiting for Hot Paths (Flash Sale)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Fixed window for general API endpoints
    options.AddFixedWindowLimiter("Global", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.QueueLimit = 2;
    });

    // Token bucket for Hot Paths (e.g., Ticket Reservation)
    options.AddTokenBucketLimiter("HotPath", limiterOptions =>
    {
        limiterOptions.TokenLimit = 5;
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        limiterOptions.TokensPerPeriod = 1;
        limiterOptions.QueueLimit = 0; // Drop requests immediately if overloaded
    });
});

// Auto-discover and register all Minimal API endpoint definitions
builder.Services.AddEndpointDefinitions();

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register gRPC with interceptors
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcServerInterceptor>();
});

// Register the gRPC service implementation also as ITicketAvailabilityNotifier (singleton).
// Domain event handlers inject this interface — decoupled from gRPC infrastructure.
builder.Services.AddSingleton<TicketGrpcServiceImpl>();
builder.Services.AddSingleton<ITicketAvailabilityNotifier>(sp => sp.GetRequiredService<TicketGrpcServiceImpl>());

// Add OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

// Enable Correlation ID tracking (add before exception handling so logs have the ID)
app.UseMiddleware<CorrelationIdMiddleware>();

// Global Exception Handling middleware
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Map modern Scalar API Documentation UI (alternative to SwaggerUI)
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("LongPd.CleanArchitecture API Documentation")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Enable gRPC-Web support
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// Map Prometheus scraping endpoint for Grafana
app.MapPrometheusScrapingEndpoint();

// Enable Rate Limiting
app.UseRateLimiter();

// Map Minimal API Endpoints
app.MapEndpointDefinitions();

// Map Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            details = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

// Map gRPC Services with gRPC-Web integration — resolved from singleton DI registration
app.MapGrpcService<TicketGrpcServiceImpl>().EnableGrpcWeb();

app.Run();

// Required for integration testing visibility
public partial class Program { }
