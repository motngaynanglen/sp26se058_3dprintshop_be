using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;

public class ResponseDesignTemplateModels
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Description { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime Created { get; init; }
    public DateTime Updated { get; init; }
    public DateTime? Deleted { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid UpdatedBy { get; init; }
    public Guid DeletedBy { get; init; }
}
