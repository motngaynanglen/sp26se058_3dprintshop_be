using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.Mainflow2;

public sealed class LinkedOrderTimelineSnapshot
{
    public Guid OrderId { get; init; }
    public string? OrderCode { get; init; }
    public string OrderStatus { get; init; } = string.Empty;
    public string? PaymentStatus { get; init; }
    public string? ShipmentStatus { get; init; }
}

public static class Mainflow2TimelineBuilder
{
    public static IReadOnlyList<Mainflow2TimelineStepDto> Build(
        DesignWork dw,
        LinkedOrderTimelineSnapshot? linkedOrder = null,
        bool designReadyForBalance = false)
    {
        if (dw.Status == Mainflow2DesignWorkStatuses.Cancelled)
        {
            return new[]
            {
                new Mainflow2TimelineStepDto
                {
                    Code = "CANCELLED",
                    Label = "Đã hủy yêu cầu",
                    CompletedAt = dw.LastModified,
                    IsDone = true,
                    IsCurrent = true
                }
            };
        }

        var currentCode = ResolveCurrentStepCode(dw);
        var isFullPayFlow = OrderPaymentHelper.IsDirectPrintSourceType(dw.SourceType);

        var designSteps = new List<Mainflow2TimelineStepDto>
        {
            new Mainflow2TimelineStepDto
            {
                Code = "SUBMITTED",
                Label = "Đã gửi yêu cầu",
                CompletedAt = dw.Created,
                IsDone = true,
                IsCurrent = currentCode == "SUBMITTED"
            },
            new Mainflow2TimelineStepDto
            {
                Code = "ASSIGNED",
                Label = "Nhân viên tiếp nhận",
                CompletedAt = dw.StaffAssignedAt,
                IsDone = dw.MainAssignedStaffId != null,
                IsCurrent = currentCode == "ASSIGNED"
            },
            new Mainflow2TimelineStepDto
            {
                Code = "QUOTED",
                Label = isFullPayFlow ? "Báo giá in" : "Báo giá thiết kế + in",
                CompletedAt = dw.LastQuotedAt,
                IsDone = dw.LastQuotedAt != null,
                IsCurrent = currentCode == "QUOTED"
            },
            new Mainflow2TimelineStepDto
            {
                Code = "APPROVED",
                Label = "Khách chấp nhận báo giá",
                CompletedAt = dw.CustomerApprovedAt,
                IsDone = dw.Status == Mainflow2DesignWorkStatuses.Approved,
                IsCurrent = currentCode == "APPROVED" && linkedOrder is null
            }
        };

        if (linkedOrder is null)
            return designSteps;

        var productionCurrent = ResolveProductionStepCode(linkedOrder, designReadyForBalance);
        var isPaid = linkedOrder.PaymentStatus == InvoiceStatuses.Paid;
        var isDeposited = linkedOrder.PaymentStatus == InvoiceStatuses.PartiallyPaid;

        designSteps.Add(new Mainflow2TimelineStepDto
        {
            Code = "ORDER_PLACED",
            Label = "Đã đặt hàng",
            CompletedAt = null,
            IsDone = true,
            IsCurrent = productionCurrent == "ORDER_PLACED"
        });

        designSteps.Add(new Mainflow2TimelineStepDto
        {
            Code = "PAID",
            Label = isDeposited && !isFullPayFlow ? "Cọc 30% tiền thiết kế" : "Đã thanh toán đủ",
            CompletedAt = null,
            IsDone = isPaid || isDeposited,
            IsCurrent = productionCurrent == "PAID"
        });

        if (isDeposited && !isPaid && !isFullPayFlow)
        {
            designSteps.Add(new Mainflow2TimelineStepDto
            {
                Code = "DESIGNING",
                Label = designReadyForBalance ? "Đã gửi bảng thiết kế" : "Chờ bảng thiết kế",
                CompletedAt = null,
                IsDone = designReadyForBalance,
                IsCurrent = productionCurrent == "DESIGNING"
            });

            designSteps.Add(new Mainflow2TimelineStepDto
            {
                Code = "BALANCE_DUE",
                Label = "Hoàn tất thanh toán",
                CompletedAt = null,
                IsDone = false,
                IsCurrent = productionCurrent == "BALANCE_DUE"
            });
        }

        designSteps.Add(new Mainflow2TimelineStepDto
        {
            Code = "PROCESSING",
            Label = "Đang sản xuất / in 3D",
            CompletedAt = null,
            IsDone = isPaid && (linkedOrder.OrderStatus is OrderStatuses.Finished or OrderStatuses.Completed
                || linkedOrder.ShipmentStatus is ShipmentStatuses.ReadyForPickup
                    or ShipmentStatuses.InTransit or ShipmentStatuses.Delivered),
            IsCurrent = productionCurrent == "PROCESSING"
        });

        designSteps.Add(new Mainflow2TimelineStepDto
        {
            Code = "READY_FOR_SHIP",
            Label = "Sẵn sàng giao (chờ GHN)",
            CompletedAt = null,
            IsDone = isPaid && (linkedOrder.ShipmentStatus is ShipmentStatuses.InTransit
                or ShipmentStatuses.Delivered
                || linkedOrder.OrderStatus == OrderStatuses.Completed),
            IsCurrent = productionCurrent == "READY_FOR_SHIP"
        });

        designSteps.Add(new Mainflow2TimelineStepDto
        {
            Code = "SHIPPING",
            Label = "Đang giao hàng",
            CompletedAt = null,
            IsDone = isPaid && (linkedOrder.ShipmentStatus == ShipmentStatuses.InTransit
                || linkedOrder.ShipmentStatus == ShipmentStatuses.Delivered
                || linkedOrder.OrderStatus == OrderStatuses.Completed),
            IsCurrent = productionCurrent == "SHIPPING"
        });

        designSteps.Add(new Mainflow2TimelineStepDto
        {
            Code = "COMPLETED",
            Label = "Hoàn thành",
            CompletedAt = null,
            IsDone = linkedOrder.OrderStatus == OrderStatuses.Completed,
            IsCurrent = productionCurrent == "COMPLETED"
        });

        return designSteps;
    }

    private static string ResolveProductionStepCode(LinkedOrderTimelineSnapshot order, bool designReadyForBalance)
    {
        if (order.PaymentStatus != InvoiceStatuses.Paid
            && order.PaymentStatus != InvoiceStatuses.PartiallyPaid)
            return order.OrderId != Guid.Empty ? "ORDER_PLACED" : "APPROVED";

        if (order.PaymentStatus == InvoiceStatuses.PartiallyPaid)
        {
            if (!designReadyForBalance)
                return "DESIGNING";
            return "BALANCE_DUE";
        }

        if (order.OrderStatus == OrderStatuses.Completed)
            return "COMPLETED";

        if (order.ShipmentStatus == ShipmentStatuses.InTransit
            || order.ShipmentStatus == ShipmentStatuses.Delivered)
            return "SHIPPING";

        if (order.OrderStatus == OrderStatuses.Processing
            || order.ShipmentStatus == ShipmentStatuses.Preparing)
            return "PROCESSING";

        if (order.OrderStatus == OrderStatuses.Finished
            || order.ShipmentStatus == ShipmentStatuses.ReadyForPickup)
            return "READY_FOR_SHIP";

        return "PAID";
    }

    private static string ResolveCurrentStepCode(DesignWork dw) =>
        dw.Status switch
        {
            Mainflow2DesignWorkStatuses.Submitted => "SUBMITTED",
            Mainflow2DesignWorkStatuses.Assigned => "ASSIGNED",
            Mainflow2DesignWorkStatuses.Quoted or Mainflow2DesignWorkStatuses.Negotiating => "QUOTED",
            Mainflow2DesignWorkStatuses.Approved => "APPROVED",
            _ => "SUBMITTED"
        };
}
