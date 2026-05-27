using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ECommerce.Contracts.Interceptors;

public class DeadlinePropagationInterceptor : Interceptor
{
    private static readonly AsyncLocal<DateTime?> _deadline = new();

    public static void SetDeadline(DateTime? deadline)
    {
        _deadline.Value = deadline;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        if (!_deadline.Value.HasValue)
            return continuation(request, context);

        var options = new CallOptions(
            headers: context.Options.Headers,
            deadline: _deadline.Value.Value,
            cancellationToken: context.Options.CancellationToken,
            writeOptions: context.Options.WriteOptions,
            propagationToken: context.Options.PropagationToken,
            credentials: context.Options.Credentials);

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, options);

        return continuation(request, newContext);
    }
}
