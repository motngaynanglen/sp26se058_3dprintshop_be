using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class ExpiredPendingOrderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredPendingOrderHostedService> _logger;
    private readonly ExpiredPendingOrderJobOptions _options;

    public ExpiredPendingOrderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredPendingOrderHostedService> logger,
        IOptions<ExpiredPendingOrderJobOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CancelExpiredOrdersAsync(stoppingToken);

        var intervalMinutes = Math.Max(_options.IntervalMinutes, 1);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CancelExpiredOrdersAsync(stoppingToken);
        }
    }

    private async Task CancelExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOrderPendingService>();
            var cancelledCount = await service.CancelExpiredPendingOrdersAsync(cancellationToken);

            if (cancelledCount > 0)
            {
                _logger.LogInformation("Auto-cancelled {Count} expired pending orders.", cancelledCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-cancel expired pending orders.");
        }
    }
}
