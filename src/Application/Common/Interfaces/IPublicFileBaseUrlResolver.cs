namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>URL gốc BE để ghép đường dẫn <c>/uploads/...</c> (cấu hình hoặc host của request hiện tại).</summary>
public interface IPublicFileBaseUrlResolver
{
    string GetBaseUrl();
}
