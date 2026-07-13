using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;
public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentLink(Order order, string returnUrl, string cancelUrl);
    Task<WebhookData> VerifyWebhookData(Webhook webhook);
    Task<PayOsPaymentLinkStatusResult> GetPaymentLinkStatus(long orderCode);
    Task<bool> CancelPaymentLink(long orderCode, string? reason = null);
}
