using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class PayOsService : IPaymentService
{
    private readonly PayOSClient _payOsClient;
    private readonly PayOsCodeGenerator _codeGenerator;
    private readonly ILogger<PayOsService> _logger;

    public PayOsService(
        PayOSClient payOsClient,
        PayOsCodeGenerator codeGenerator,
        ILogger<PayOsService> logger)
    {
        _payOsClient = payOsClient;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreatePaymentLink(Order order, string returnUrl, string cancelUrl)
    {
        long orderCode = _codeGenerator.GenerateCode();
        DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes);

        var items = order.OrderItems.Select(x => new PaymentLinkItem
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
            Items = items,
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl,
            ExpiredAt = (int)expiryTime.ToUnixTimeSeconds()
        };

        CreatePaymentLinkResponse paymentResult = await _payOsClient.PaymentRequests.CreateAsync(paymentData);

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
            return await _payOsClient.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception)
        {
            // Invalid signature — treat as tampered payload.
            throw new BusinessException("Chữ ký webhook không hợp lệ.");
        }
    }

    public async Task<PayOsPaymentLinkStatusResult> GetPaymentLinkStatus(long orderCode)
    {
        var paymentLink = await _payOsClient.PaymentRequests.GetAsync(orderCode);
        var latestTransaction = paymentLink.Transactions?.LastOrDefault();

        return new PayOsPaymentLinkStatusResult
        {
            OrderCode = paymentLink.OrderCode,
            Amount = paymentLink.Amount,
            AmountPaid = paymentLink.AmountPaid,
            AmountRemaining = paymentLink.AmountRemaining,
            Status = paymentLink.Status.ToString(),
            PaymentLinkId = paymentLink.Id,
            Reference = latestTransaction?.Reference,
            TransactionDateTime = latestTransaction?.TransactionDateTime
        };
    }

    public async Task<bool> CancelPaymentLink(long orderCode, string? reason = null)
    {
        try
        {
            await _payOsClient.PaymentRequests.CancelAsync(orderCode, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not cancel PayOS payment link for order code {OrderCode}.", orderCode);
            return false;
        }
    }
}
