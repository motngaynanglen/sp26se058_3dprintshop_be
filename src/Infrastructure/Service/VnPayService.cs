using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class VnPayService : IVnPayService
{
    private readonly VnPaySettings _settings;
    private readonly PayOsCodeGenerator _codeGenerator;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(
        IOptions<VnPaySettings> settings,
        PayOsCodeGenerator codeGenerator,
        ILogger<VnPayService> logger)
    {
        _settings = settings.Value;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }

    public PaymentResponse CreatePaymentUrl(Order order, string clientIp)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException(
                "VNPay Sandbox chưa được cấu hình. Điền TmnCode và HashSecret trong appsettings (mục VNPay).");

        var txnRef = _codeGenerator.GenerateCode().ToString();
        // Số tiền phải trả gồm cả phí ship (lưu ở Invoice).
        var payable = order.Invoice?.TotalAmount ?? order.TotalPrice;
        var amount = (long)Math.Round(payable * 100m, 0, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            throw new InvalidOperationException("Số tiền thanh toán VNPay phải lớn hơn 0.");

        var now = CoreHelper.SystemTimeNow;
        var createDate = VnPayHelper.FormatVnPayDate(now);
        var expireDate = VnPayHelper.FormatVnPayDate(now.AddMinutes(15));
        var returnUrl = ResolveReturnUrl();
        var ip = NormalizeClientIp(clientIp);

        var vnpParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode.Trim(),
            ["vnp_Amount"] = amount.ToString(),
            ["vnp_CreateDate"] = createDate,
            ["vnp_ExpireDate"] = expireDate,
            ["vnp_CurrCode"] = _settings.CurrCode,
            ["vnp_IpAddr"] = ip,
            ["vnp_Locale"] = _settings.Locale,
            ["vnp_OrderInfo"] = VnPayHelper.SanitizeOrderInfo(order.Code),
            ["vnp_OrderType"] = _settings.OrderType,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = txnRef,
        };

        var signData = VnPayHelper.BuildSignData(vnpParams);
        var paymentUrl = VnPayHelper.BuildPaymentUrl(
            _settings.PaymentUrl,
            _settings.HashSecret,
            vnpParams);

        _logger.LogInformation(
            "[VNPay] Tạo URL thanh toán — OrderCode={OrderCode} TxnRef={TxnRef} Amount={Amount} TmnCode={TmnCode} SignData={SignData} PaymentUrl={PaymentUrl}",
            order.Code,
            txnRef,
            amount,
            _settings.TmnCode.Trim(),
            signData,
            paymentUrl);

        return new PaymentResponse
        {
            OrderId = order.Id.ToString(),
            PaymentCode = long.Parse(txnRef),
            PaymentLink = paymentUrl,
            QrCode = string.Empty,
            ExpiredAt = CoreHelper.SystemTimeNow.AddMinutes(15),
        };
    }

    public VnPayCallbackResult VerifyCallback(IReadOnlyDictionary<string, string> queryParams)
    {
        var dict = VnPayHelper.ParseQueryToDictionary(queryParams);

        if (!dict.TryGetValue("vnp_SecureHash", out var incomingHash) || string.IsNullOrEmpty(incomingHash))
            return new VnPayCallbackResult(false, false, "", null, null, "Thiếu vnp_SecureHash");

        var signData = VnPayHelper.BuildSignData(dict);
        var computed = VnPayHelper.HmacSha512(_settings.HashSecret.Trim(), signData);
        var isValid = string.Equals(computed, incomingHash, StringComparison.OrdinalIgnoreCase);

        dict.TryGetValue("vnp_TxnRef", out var txnRef);
        dict.TryGetValue("vnp_ResponseCode", out var responseCode);
        dict.TryGetValue("vnp_TransactionNo", out var transNo);
        dict.TryGetValue("vnp_TransactionStatus", out var transStatus);

        var success = isValid
                      && responseCode == "00"
                      && (string.IsNullOrEmpty(transStatus) || transStatus == "00");

        return new VnPayCallbackResult(
            isValid,
            success,
            txnRef ?? "",
            responseCode,
            transNo,
            success ? "Thanh toán VNPay thành công" : $"VNPay ResponseCode={responseCode}");
    }

    private string ResolveReturnUrl()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ReturnUrl))
            return _settings.ReturnUrl.Trim();

        var apiBase = _settings.ApiBaseUrl.TrimEnd('/');
        return $"{apiBase}/api/transaction/vnpay-return";
    }

    private static string NormalizeClientIp(string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp)
            || clientIp == "0.0.0.1"
            || clientIp == "::1"
            || clientIp.StartsWith("::ffff:127.", StringComparison.OrdinalIgnoreCase))
            return "127.0.0.1";
        return clientIp;
    }
}
