using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public interface IVnPayService
{
    /// <summary>Tạo URL redirect sang cổng VNPay Sandbox.</summary>
    PaymentResponse CreatePaymentUrl(Order order, string clientIp);

    /// <summary>Xác thực callback (return URL / IPN) và trả TxnRef + mã phản hồi.</summary>
    VnPayCallbackResult VerifyCallback(IReadOnlyDictionary<string, string> queryParams);
}

public record VnPayCallbackResult(
    bool IsValidSignature,
    bool IsSuccess,
    string TxnRef,
    string? ResponseCode,
    string? TransactionNo,
    string? Message);
