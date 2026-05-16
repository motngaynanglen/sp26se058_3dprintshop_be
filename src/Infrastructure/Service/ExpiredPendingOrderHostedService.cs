using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class ExpiredPendingOrderHostedService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredPendingOrderHostedService> _logger;

    public ExpiredPendingOrderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredPendingOrderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CancelExpiredOrdersAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
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
