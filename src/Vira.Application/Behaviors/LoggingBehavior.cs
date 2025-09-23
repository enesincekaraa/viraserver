using MediatR;
using Microsoft.Extensions.Logging;

namespace Vira.Application.Behaviors;

public sealed class LoggingBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes>
    where TReq : IRequest<TRes>   // <-- ÖNEMLİ
{
    private readonly ILogger<LoggingBehavior<TReq, TRes>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TReq, TRes>> logger) => _logger = logger;

    public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Request} {@Payload}", typeof(TReq).Name, request);
        var response = await next();
        _logger.LogInformation("Handled {Request}", typeof(TReq).Name);
        return response;
    }
}
