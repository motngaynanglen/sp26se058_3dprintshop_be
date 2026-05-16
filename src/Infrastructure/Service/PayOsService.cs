using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;


namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class PayOsService : IPaymentService
{
    private readonly PayOSClient _payOsClient;
    private readonly PayOsCodeGenerator _codeGenerator;

    public PayOsService(PayOSClient payOsClient, PayOsCodeGenerator codeGenerator)
    {
        _payOsClient = payOsClient;
        _codeGenerator = codeGenerator;
    }
    public async Task<PaymentResponse> CreatePaymentLink(Order order, string returnUrl, string cancelUrl)
    {
        // Tạo mã thanh toán duy nhất (số)
        long orderCode = _codeGenerator.GenerateCode();
        DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes);

        List<PaymentLinkItem> Items = order.OrderItems.Select(x => new PaymentLinkItem
        {
            Name = x.ItemName ?? "Sản phẩm.",
            Quantity = x.QuantityOrdered,
            Price = (int)x.UnitPrice
        }).ToList();

        var paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = (int)order.TotalPrice,
            Description = $"#{order.Code}",
            Items = Items,
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl,
            ExpiredAt = (int)expiryTime.ToUnixTimeSeconds()
        };

        // Gọi API PayOS
        CreatePaymentLinkResponse paymentResult = await _payOsClient.PaymentRequests.CreateAsync(paymentData);

        // Trả về object đầy đủ như bạn muốn
        return new PaymentResponse
        {
            OrderId = order.Id.ToString(),
            PaymentCode = orderCode,
            PaymentLink = paymentResult.CheckoutUrl,
            QrCode = paymentResult.QrCode,
            ExpiredAt = expiryTime.UtcToOffsetSystemTime(),
        };
    }

    public async Task<WebhookData> VerifyWebhookData(Webhook webhook)
    {
        try
        {
            WebhookData verifiedData = await _payOsClient.Webhooks.VerifyAsync(webhook);
            return verifiedData;
        }
        catch (Exception)
        {
            // Nếu sai chữ ký, coi như dữ liệu không hợp lệ
            throw new Exception("Chữ ký Webhook không hợp lệ!");
        }
    }
    public async Task<bool> CancelPaymentLink(long orderCode, string? reason = null)
    {
        try
        {
            // Gọi API của PayOS để hủy link thanh toán
            // Lý do hủy là tùy chọn
            var result = await _payOsClient.PaymentRequests.CancelAsync(orderCode, reason);

            // Nếu không có exception ném ra, PayOS coi như đã xử lý yêu cầu hủy thành công
            return true;
        }
        catch (Exception ex)
        {
            // Log lỗi nếu cần thiết (Ví dụ: Link đã hết hạn hoặc đã hủy rồi PayOS sẽ báo lỗi)
            Console.WriteLine($"PayOS Cancel Error for OrderCode {orderCode}: {ex.Message}");
            // return để logic ở Handler phía sau tiếp tục chạy.
            return false;
        }
    }
}
