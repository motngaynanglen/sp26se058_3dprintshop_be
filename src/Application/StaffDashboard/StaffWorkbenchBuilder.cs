using sp26se058_3dprintshop_be.Application.StaffDashboard.Models;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.StaffDashboard;

internal sealed class StaffWorkbenchContext
{
    public required IReadOnlyList<DesignWork> Mf2Designs { get; init; }
    public required IReadOnlyList<Order> Orders { get; init; }
    public required IReadOnlyDictionary<Guid, Shipment> ShipmentsByOrderId { get; init; }
    public int ProductionQueueCount { get; init; }
    public IReadOnlyList<(Guid Id, string Code)> ProductionQueueOrders { get; init; } = Array.Empty<(Guid, string)>();
    public Guid? CurrentStaffId { get; init; }
    public bool IsManager { get; init; }
}

internal static class StaffWorkbenchBuilder
{
    private static readonly HashSet<string> CustomSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SourceTypes.CustomFilePrintMainflow2,
        SourceTypes.CustomQuoteMainflow2,
        SourceTypes.AiGenerated,
        SourceTypes.PreOrder
    };

    private static readonly HashSet<string> Mf2ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        Mainflow2DesignWorkStatuses.Submitted,
        Mainflow2DesignWorkStatuses.Assigned,
        Mainflow2DesignWorkStatuses.Quoted,
        Mainflow2DesignWorkStatuses.Negotiating
    };

    public const int Mf2AssignHours = 4;
    public const int ProductionStaleHours = 48;
    public const int GhnAfterFinishedHours = 24;

    private static string Mf2Href(Guid id) => $"/staff/custom-orders/{id}"; // MF2 + Flow 3 AI
    private static string ShopOrderHref(Guid id) => $"/staff/shop-orders?openOrderId={id}";
    private static string ProductionHref(Guid id) => $"/staff/production-queue?orderId={id}";

    public static StaffWorkbenchDto Build(StaffWorkbenchContext ctx)
    {
        var now = DateTimeOffset.UtcNow;
        var tasks = new List<StaffWorkbenchTaskDto>();

        var mf2Submitted = ctx.Mf2Designs
            .Where(d => d.Status == Mainflow2DesignWorkStatuses.Submitted)
            .ToList();
        var mf2Overdue = mf2Submitted
            .Where(d => HoursSince(d.Created, now) >= Mf2AssignHours)
            .ToList();

        var mf2AssignedNoQuote = ctx.Mf2Designs
            .Where(d => d.Status == Mainflow2DesignWorkStatuses.Assigned
                        && (d.QuoteRevision == 0 || d.LatestQuotedPrice == null))
            .Where(d => ctx.IsManager || d.MainAssignedStaffId == ctx.CurrentStaffId)
            .ToList();

        var mf2AwaitingCustomer = ctx.Mf2Designs
            .Where(d => d.Status is Mainflow2DesignWorkStatuses.Quoted or Mainflow2DesignWorkStatuses.Negotiating)
            .Where(d => ctx.IsManager || d.MainAssignedStaffId == ctx.CurrentStaffId)
            .ToList();

        var readyGhn = new List<(Order Order, Shipment? Ship)>();
        var productionStale = new List<Order>();
        var notStartedPrint = new List<Order>();
        var pendingPay = new List<Order>();
        var pendingConfirm = new List<Order>();

        foreach (var order in ctx.Orders)
        {
            ctx.ShipmentsByOrderId.TryGetValue(order.Id, out var ship);
            var paid = OrderPaymentHelper.CanProceedToFulfillment(order.Invoice);

            if (order.OrderStatus == OrderStatuses.Pending && paid)
                pendingConfirm.Add(order);

            if (order.OrderStatus == OrderStatuses.Finished
                && paid
                && string.IsNullOrWhiteSpace(ship?.CarrierOrderCode))
            {
                readyGhn.Add((order, ship));
            }

            if (order.OrderStatus == OrderStatuses.Pending && !paid)
                pendingPay.Add(order);

            if (order.OrderStatus != OrderStatuses.Processing || !paid)
                continue;

            var customItems = order.OrderItems
                .Where(oi => CustomSourceTypes.Contains(oi.SourceType))
                .ToList();
            if (customItems.Count == 0)
                continue;

            var anchor = order.DepositedAt ?? order.Created;
            var hours = HoursSince(anchor, now);
            var hasPendingProduction = customItems.Any(oi =>
                oi.FulfillmentStatus != OrderItemStatuses.Finished
                && oi.FulfillmentStatus != OrderItemStatuses.Cancelled);

            if (hasPendingProduction && hours >= ProductionStaleHours)
                productionStale.Add(order);

            if (customItems.Any(oi =>
                    (oi.FulfillmentStatus == OrderItemStatuses.Pending
                     || oi.FulfillmentStatus == OrderItemStatuses.Designing)
                    && oi.FulfillmentStatus != OrderItemStatuses.Cancelled))
            {
                notStartedPrint.Add(order);
            }
        }

        var ghnOverdue = readyGhn
            .Where(x => HoursSince(x.Order.LastModified, now) >= GhnAfterFinishedHours)
            .ToList();

        if (mf2Overdue.Count > 0)
        {
            tasks.Add(Task(
                "mf2-overdue", 0, "critical",
                StaffWorkbenchTaskTypes.OrderConfirm,
                "Yêu cầu custom quá hạn tiếp nhận",
                $"Khách gửi từ ≥ {Mf2AssignHours}h — cần tiếp nhận & báo giá sớm.",
                mf2Overdue.Count, "/staff/custom-orders", "Xử lý ngay",
                mf2Overdue.Select(d => Item(d.Id, d.Name ?? d.Id.ToString(),
                    $"{(int)HoursSince(d.Created, now)}h trước", Mf2Href(d.Id))).ToList()));
        }
        else if (mf2Submitted.Count > 0)
        {
            tasks.Add(Task(
                "mf2-new", 1, "high",
                StaffWorkbenchTaskTypes.OrderConfirm,
                "Yêu cầu custom mới — chưa tiếp nhận",
                "Tiếp nhận → trao đổi → gửi báo giá cho khách.",
                mf2Submitted.Count, "/staff/custom-orders", "Tiếp nhận",
                mf2Submitted.Select(d => Item(d.Id, d.Name ?? d.Id.ToString(),
                    d.SourceType == SourceTypes.CustomFilePrintMainflow2 ? "File in" : "Báo giá", Mf2Href(d.Id))).ToList()));
        }

        if (pendingConfirm.Count > 0)
        {
            tasks.Add(Task(
                "order-confirm", 1, "high",
                StaffWorkbenchTaskTypes.OrderConfirm,
                "Đơn đã thanh toán — chờ xác nhận",
                "Tiếp nhận đơn (COD hoặc đã TT) để chuyển sang xử lý / sản xuất.",
                pendingConfirm.Count, "/staff/shop-orders", "Xác nhận đơn",
                pendingConfirm.Take(5).Select(o => Item(o.Id, o.Code, "Chờ xác nhận", ShopOrderHref(o.Id))).ToList()));
        }

        if (ctx.ProductionQueueCount > 0)
        {
            var pqItems = ctx.ProductionQueueOrders.Take(5)
                .Select(o => Item(o.Id, o.Code, "Đang SX", ProductionHref(o.Id)))
                .ToList();
            tasks.Add(Task(
                "production-queue",
                mf2Overdue.Count > 0 ? 1 : 0,
                mf2Overdue.Count > 0 ? "high" : "critical",
                StaffWorkbenchTaskTypes.Production,
                "Hàng đợi sản xuất / in 3D",
                "Đơn đã thanh toán — hoàn tất in từng sản phẩm trước khi giao.",
                ctx.ProductionQueueCount, "/staff/production-queue", "Vào xưởng in", pqItems));
        }

        if (productionStale.Count > 0)
        {
            tasks.Add(Task(
                "production-stale", 0, "critical",
                StaffWorkbenchTaskTypes.Production,
                "Sản xuất chậm — chưa hoàn thiện in",
                $"Đơn đã TT > {ProductionStaleHours}h vẫn chưa in xong.",
                productionStale.Count, "/staff/production-queue", "Ưu tiên in",
                productionStale.Take(5).Select(o => Item(o.Id, o.Code,
                    $"{(int)HoursSince(o.DepositedAt ?? o.Created, now)}h", ProductionHref(o.Id))).ToList()));
        }

        if (ghnOverdue.Count > 0)
        {
            tasks.Add(Task(
                "ghn-overdue", 0, "critical",
                StaffWorkbenchTaskTypes.Shipping,
                "Chậm tạo vận đơn GHN",
                "Sản xuất xong nhưng chưa bàn giao ship — ảnh hưởng cam kết giao.",
                ghnOverdue.Count, "/staff/shop-orders", "Tạo GHN",
                ghnOverdue.Take(5).Select(x => Item(x.Order.Id, x.Order.Code, "Sẵn sàng giao", ShopOrderHref(x.Order.Id))).ToList()));
        }
        else if (readyGhn.Count > 0)
        {
            tasks.Add(Task(
                "ghn-ready", 2, "medium",
                StaffWorkbenchTaskTypes.Shipping,
                "Sẵn sàng giao — chờ tạo GHN",
                "Đã in xong & chuyển FINISHED — tạo vận đơn để GHN lấy hàng.",
                readyGhn.Count, "/staff/shop-orders", "Tạo vận đơn",
                readyGhn.Take(5).Select(x => Item(x.Order.Id, x.Order.Code, "Chờ GHN", ShopOrderHref(x.Order.Id))).ToList()));
        }

        if (mf2AssignedNoQuote.Count > 0)
        {
            tasks.Add(Task(
                "mf2-quote", 2, "high",
                StaffWorkbenchTaskTypes.Quote,
                "Đã nhận việc — chưa gửi báo giá",
                "Khách đang chờ báo giá để duyệt & thanh toán.",
                mf2AssignedNoQuote.Count, "/staff/custom-orders", "Báo giá",
                mf2AssignedNoQuote.Take(5).Select(d => Item(d.Id, d.Name ?? d.Id.ToString(), "Đã assign", Mf2Href(d.Id))).ToList()));
        }

        if (notStartedPrint.Count > 0 && ctx.ProductionQueueCount == 0)
        {
            tasks.Add(Task(
                "print-start", 2, "medium",
                StaffWorkbenchTaskTypes.Production,
                "Đơn mới TT — chưa bắt đầu in",
                "Vào hàng đợi SX, bấm «Bắt đầu in» cho từng dòng.",
                notStartedPrint.Count, "/staff/production-queue", "Bắt đầu in",
                notStartedPrint.Take(5).Select(o => Item(o.Id, o.Code, "Chưa in", ProductionHref(o.Id))).ToList()));
        }

        if (mf2AwaitingCustomer.Count > 0)
        {
            tasks.Add(Task(
                "mf2-wait-customer", 3, "low",
                StaffWorkbenchTaskTypes.FollowUp,
                "Chờ khách duyệt báo giá",
                "Theo dõi chat — nhắc khách nếu lâu không phản hồi.",
                mf2AwaitingCustomer.Count, "/staff/custom-orders", "Xem",
                mf2AwaitingCustomer.Take(5).Select(d => Item(d.Id, d.Name ?? d.Id.ToString(), d.Status, Mf2Href(d.Id))).ToList()));
        }

        if (pendingPay.Count > 0)
        {
            tasks.Add(Task(
                "pending-pay", 4, "low",
                StaffWorkbenchTaskTypes.FollowUp,
                "Đơn chờ khách thanh toán",
                "Chưa cần sản xuất — theo dõi nếu khách hỏi.",
                pendingPay.Count, "/staff/shop-orders", "Xem",
                pendingPay.Take(5).Select(o => Item(o.Id, o.Code, "Chờ TT", ShopOrderHref(o.Id))).ToList()));
        }

        tasks.Sort((a, b) =>
        {
            var byPriority = StaffWorkbenchPriorityLevels.SortOrder(a.PriorityLevel)
                .CompareTo(StaffWorkbenchPriorityLevels.SortOrder(b.PriorityLevel));
            return byPriority != 0 ? byPriority : a.Priority.CompareTo(b.Priority);
        });

        var critical = tasks.Count(t => t.Severity == "critical");
        var high = tasks.Count(t => t.Severity == "high");
        var total = tasks.Sum(t => t.Count);

        var mf2Pending = ctx.Mf2Designs.Count(d => Mf2ActiveStatuses.Contains(d.Status));

        return new StaffWorkbenchDto
        {
            Sla = new StaffWorkbenchSlaDto
            {
                Mf2AssignHours = Mf2AssignHours,
                ProductionStaleHours = ProductionStaleHours,
                GhnAfterFinishedHours = GhnAfterFinishedHours
            },
            Counts = new StaffWorkbenchCountsDto
            {
                ProductionQueueCount = ctx.ProductionQueueCount,
                Mf2Submitted = mf2Submitted.Count,
                Mf2Pending = mf2Pending,
                OrdersReadyGhn = readyGhn.Count
            },
            Health = new StaffWorkbenchHealthDto
            {
                Critical = critical,
                High = high,
                Total = total,
                AllClear = tasks.Count == 0 || (critical == 0 && high == 0)
            },
            Tasks = tasks
        };
    }

    private static double HoursSince(DateTimeOffset at, DateTimeOffset now) =>
        Math.Max(0, (now - at).TotalHours);

    private static StaffWorkbenchTaskItemDto Item(Guid key, string label, string meta, string href) =>
        new() { Key = key, Label = label, Meta = meta, Href = href };

    private static StaffWorkbenchTaskDto Task(
        string id,
        int priority,
        string severity,
        string taskType,
        string title,
        string description,
        int count,
        string href,
        string actionLabel,
        IReadOnlyList<StaffWorkbenchTaskItemDto>? items = null)
    {
        var list = items ?? Array.Empty<StaffWorkbenchTaskItemDto>();
        var primary = list.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.Href))?.Href;
        var priorityLevel = StaffWorkbenchPriorityLevels.FromSeverity(severity);
        return new StaffWorkbenchTaskDto
        {
            Id = id,
            Priority = priority,
            Severity = severity,
            TaskType = taskType,
            TaskTypeLabel = StaffWorkbenchTaskTypes.Label(taskType),
            PriorityLevel = priorityLevel,
            PriorityLabel = StaffWorkbenchPriorityLevels.Label(priorityLevel),
            Title = title,
            Description = description,
            Count = count,
            Href = href,
            PrimaryHref = primary ?? href,
            ActionLabel = actionLabel,
            Items = list
        };
    }
}
