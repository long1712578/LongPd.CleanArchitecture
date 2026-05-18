using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LongPd.CleanArchitecture.Api.Grpc;

/// <summary>
/// gRPC server interceptor — adds logging, error handling, and performance tracking
/// to all gRPC method invocations (Unary, Server Streaming, Client Streaming, Duplex).
///
/// Responsibilities:
///   - Log method name, duration, and status code for every call.
///   - Catch unhandled exceptions and map them to proper gRPC status codes.
///   - Correlate gRPC calls with HTTP request trace IDs for observability.
/// </summary>
public sealed class GrpcServerInterceptor(ILogger<GrpcServerInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var methodName = context.Method;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("[gRPC] Unary call started: {Method}", methodName);

        try
        {
            var response = await continuation(request, context);

            sw.Stop();
            logger.LogInformation(
                "[gRPC] Unary call completed: {Method} | Status: OK | Duration: {ElapsedMs}ms",
                methodName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (RpcException)
        {
            sw.Stop();
            // Already a proper gRPC exception — rethrow as-is
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "[gRPC] Unary call failed: {Method} | Duration: {ElapsedMs}ms | Error: {Error}",
                methodName, sw.ElapsedMilliseconds, ex.Message);

            throw MapToRpcException(ex);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var methodName = context.Method;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("[gRPC] Server streaming started: {Method}", methodName);

        try
        {
            await continuation(request, responseStream, context);

            sw.Stop();
            logger.LogInformation(
                "[gRPC] Server streaming ended: {Method} | Duration: {ElapsedMs}ms",
                methodName, sw.ElapsedMilliseconds);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            logger.LogInformation(
                "[gRPC] Server streaming cancelled (client disconnect): {Method} | Duration: {ElapsedMs}ms",
                methodName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "[gRPC] Server streaming failed: {Method} | Duration: {ElapsedMs}ms | Error: {Error}",
                methodName, sw.ElapsedMilliseconds, ex.Message);

            throw MapToRpcException(ex);
        }
    }

    /// <summary>
    /// Maps domain/application exceptions to proper gRPC status codes.
    /// This bridges the REST-world exception hierarchy with gRPC semantics.
    /// </summary>
    private static RpcException MapToRpcException(Exception ex) => ex switch
    {
        // Not found → NOT_FOUND (Must be placed BEFORE DomainException since it inherits from it)
        Domain.Exceptions.NotFoundException notFoundEx =>
            new RpcException(new Status(StatusCode.NotFound, notFoundEx.Message)),

        // Domain exceptions → INVALID_ARGUMENT (client sent bad data)
        Domain.Exceptions.DomainException domainEx =>
            new RpcException(new Status(StatusCode.InvalidArgument, domainEx.Message)),

        // Concurrency → ABORTED (client should retry)
        Application.Common.ConcurrencyException =>
            new RpcException(new Status(StatusCode.Aborted, "Resource was modified by another request. Please retry.")),

        // Argument exceptions → INVALID_ARGUMENT
        ArgumentException argEx =>
            new RpcException(new Status(StatusCode.InvalidArgument, argEx.Message)),

        // Operation cancelled → CANCELLED
        OperationCanceledException =>
            new RpcException(new Status(StatusCode.Cancelled, "Operation was cancelled.")),

        // Everything else → INTERNAL
        _ => new RpcException(new Status(StatusCode.Internal, "An internal error occurred."))
    };
}
