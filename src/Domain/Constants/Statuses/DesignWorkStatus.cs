using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;

public static class DesignWorkStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Reviewing = "Reviewing";
    public const string Completed = "Completed";

    public static readonly List<StatusDefinition> All = new()
    {
        new(Pending, "Đang chờ", "", ""),
        new(InProgress, "Đang thiết kế, ", "", ""),
        new(Reviewing, "Đang xem xét, ", "", ""),
        new(Completed, "Hoàn thành", "", "")
    };
    
}
