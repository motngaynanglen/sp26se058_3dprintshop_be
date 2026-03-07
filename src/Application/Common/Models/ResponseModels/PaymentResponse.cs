using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
public class PaymentResponse
{
    public string OrderId { get; set; } = string.Empty;
    public long PaymentCode { get; set; } // Chính là orderCode gửi sang PayOS
    public string PaymentLink { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public DateTimeOffset ExpiredAt { get; set; }
}
