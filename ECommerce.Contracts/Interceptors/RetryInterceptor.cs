using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ECommerce.Contracts.Interceptors;

public class RetryInterceptor : Interceptor
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] Backoff = [
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(300),
        TimeSpan.FromMilliseconds(900)
    ];

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var attempt = 0;
        AsyncUnaryCall<TResponse>? lastCall = null;

        async Task<TResponse> RetryAsync()
        {
            while (true)
            {
                try
                {
                    lastCall = continuation(request, context);
                    return await lastCall.ResponseAsync;
                }
                catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
                {
                    attempt++;
                    if (attempt > MaxRetries)
                        throw;
                    await Task.Delay(Backoff[attempt - 1]);
                }
            }
        }

        var responseTask = RetryAsync();
        var headersTask = Task.Run(async () =>
        {
            await responseTask;
            return lastCall?.ResponseHeadersAsync.Result ?? new Metadata();
        });

        Task<Metadata> GetHeaders() => lastCall?.ResponseHeadersAsync ?? Task.FromResult(new Metadata());

        return new AsyncUnaryCall<TResponse>(
            responseTask,
            GetHeaders(),
            () => lastCall is not null ? lastCall.GetStatus() : new Status(StatusCode.OK, ""),
            () => lastCall is not null ? lastCall.GetTrailers() : new Metadata(),
            () => lastCall?.Dispose());
    }
}
