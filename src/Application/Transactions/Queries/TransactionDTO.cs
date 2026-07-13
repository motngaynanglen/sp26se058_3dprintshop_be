using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;

namespace sp26se058_3dprintshop_be.Application.Transactions.Queries;
public class TransactionDTO 
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public required string PaymentMethod { get; set; } // PAYOS, VNPAY, BankTransfer, Cash
    public required string InternalCode { get; set; } // Đây là mã tham chiếu của hệ thống tạo và gắn lên, hỗ trợ để đối chiếu với giao dịch
    public string? ExternalTransactionId { get; set; } // Mã giao dịch từ cổng thanh toán (ví dụ: mã tham chiếu MoMo)
    public string? Note { get; set; }
    public string? TransactionStatus { get; set; } // Pending, Success, Failed, cancelled
    public string? PaymentLink { get; set; }
    public string? QrCode { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.Transaction, TransactionDTO>();
        }

    }
}
