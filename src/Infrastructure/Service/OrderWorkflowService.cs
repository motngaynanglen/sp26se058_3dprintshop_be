using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class OrderWorkflowService : IOrderWorkflowService
{
    private readonly IApplicationDbContext _context;

    public OrderWorkflowService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ActivateOrderWorkflowAsync(Order order, CancellationToken ct)
    {
        order.OrderStatus = OrderStatuses.Processing;
        order.DepositedAt = CoreHelper.SystemTimeNow;
        order.LastModified = CoreHelper.SystemTimeNow;
        order.LastModifiedBy = "SYSTEM_AUTO";

        foreach (var item in order.OrderItems)
        {
            item.LastModified = CoreHelper.SystemTimeNow;
            item.LastModifiedBy = "SYSTEM_AUTO";

            switch (item.SourceType)
            {
                case SourceTypes.InStock:
                    item.FulfillmentStatus = OrderItemStatuses.Picking;
                    break;

                case SourceTypes.DesignService:
                    item.FulfillmentStatus = OrderItemStatuses.Designing;

                    if (item.DesignWorkId.HasValue)
                    {
                        var designWork = await _context.DesignWorks
                            .FirstOrDefaultAsync(dw => dw.Id == item.DesignWorkId, ct);

                        if (designWork != null)
                        {
                            designWork.Status = DesignWorkStatus.InProgress;

                            _context.DesignLogs.Add(new DesignLog
                            {
                                Id = Guid.NewGuid(),
                                DesignWorkId = designWork.Id,
                                LogType = DesignLogType.StatusChange,
                                Content = $"Hệ thống: Xác nhận thanh toán cho đơn {order.Code}. Quy trình thiết kế bắt đầu.",
                                Created = CoreHelper.SystemTimeNow
                            });
                        }
                    }
                    break;

                case SourceTypes.PreOrder:
                case SourceTypes.PrintService:
                    item.FulfillmentStatus = OrderItemStatuses.Printing;
                    break;
            }
        }
    }
}
