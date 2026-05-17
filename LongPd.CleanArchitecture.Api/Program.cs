using LongPd.CleanArchitecture.Api.Extensions;
using LongPd.CleanArchitecture.Api.Grpc;
using LongPd.CleanArchitecture.Api.Middleware;
using LongPd.CleanArchitecture.Api.Services;
using LongPd.CleanArchitecture.Application;
using LongPd.CleanArchitecture.Application.Abstractions.Services;
using LongPd.CleanArchitecture.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── API Presentation Layer Concerns ───────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Auto-discover and register all Minimal API endpoint definitions
builder.Services.AddEndpointDefinitions();

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddGrpc();

// Add OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

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

// Enable gRPC-Web support
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// Map Minimal API Endpoints
app.MapEndpointDefinitions();

// Map gRPC Services with gRPC-Web integration
app.MapGrpcService<TicketGrpcServiceImpl>().EnableGrpcWeb();

app.Run();

// Required for integration testing visibility
public partial class Program { }
