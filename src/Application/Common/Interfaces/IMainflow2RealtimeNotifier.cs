namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>
/// Phát sự kiện realtime cho một DesignWork (Mainflow 2 / chat luồng 2 & 3).
/// Triển khai bằng SignalR ở Web (<c>Mainflow2RealtimeNotifier</c>) hoặc no-op ở Infrastructure.
/// </summary>
public interface IMainflow2RealtimeNotifier
{
    Task NotifyAsync(Guid designWorkId, string eventName, object payload, CancellationToken cancellationToken = default);
}
