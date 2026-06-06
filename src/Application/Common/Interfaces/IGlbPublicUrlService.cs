namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>Đăng GLB lên storage công khai — B2 nếu cấu hình, không thì wwwroot.</summary>
public interface IGlbPublicUrlService
{
    Task<string> PublishGlbAsync(byte[] glbData, string folder = "models", CancellationToken cancellationToken = default);
}
