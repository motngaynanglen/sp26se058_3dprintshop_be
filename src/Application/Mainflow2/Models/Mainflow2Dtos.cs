namespace sp26se058_3dprintshop_be.Application.Mainflow2.Models;

/// <summary>Mục trong danh sách yêu cầu (list) Mainflow 2.</summary>
public class Mainflow2DesignListItemDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? LatestQuotedPrice { get; set; }
    public int QuoteRevision { get; set; }
    public DateTimeOffset Created { get; set; }
    public Guid? MainAssignedStaffId { get; set; }
    /// <summary>Phân biệt giữa các nhánh: CUSTOM_QUOTE_MF2 (mô tả) hay CUSTOM_FILE_PRINT_MF2 (STL upload).</summary>
    public string? SourceType { get; set; }
    public string? CustomerFileUrl { get; set; }
}

/// <summary>Chi tiết yêu cầu — bao gồm thread chat + version files + timeline.</summary>
public class Mainflow2DesignDetailDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public string? RequirementBrief { get; set; }
    public IReadOnlyList<string> InitialIdeaImageUrls { get; set; } = Array.Empty<string>();

    /// <summary>Với luồng 2 STL upload: URL file gốc khách gửi lên.</summary>
    public string? CustomerFileUrl { get; set; }
    /// <summary>Vật liệu khách chọn (nếu có).</summary>
    public Guid? RequestedMaterialId { get; set; }
    public string? RequestedMaterialName { get; set; }
    /// <summary>Yêu cầu kỹ thuật khác (resolution, layer height, ghi chú).</summary>
    public string? TechnicalRequirements { get; set; }

    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? MainAssignedStaffId { get; set; }
    public string? MainAssignedStaffName { get; set; }

    public decimal? LatestQuotedPrice { get; set; }
    /// <summary>Tiền thiết kế trong báo giá mới nhất (laborCost).</summary>
    public decimal? LatestQuoteDesignFee { get; set; }
    /// <summary>Tổng vật liệu trong báo giá mới nhất.</summary>
    public decimal? LatestQuoteMaterialSubtotal { get; set; }
    public int QuoteRevision { get; set; }
    public DateTimeOffset? StaffAssignedAt { get; set; }
    public DateTimeOffset? LastQuotedAt { get; set; }
    public DateTimeOffset? CustomerApprovedAt { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastModified { get; set; }

    /// <summary>Đơn hàng đã được tạo từ DesignWork này (nếu có).</summary>
    public Guid? OrderId { get; set; }

    public string? LinkedOrderCode { get; set; }

    public string? LinkedOrderStatus { get; set; }

    public string? LinkedPaymentStatus { get; set; }

    public string? LinkedShipmentStatus { get; set; }

    public string? LinkedTrackingNumber { get; set; }

    public bool DesignReadyForBalance { get; set; }

    public decimal? RemainingBalance { get; set; }

    /// <summary>GLB xem trước báo giá mới nhất (URL đã resolve — dùng cho model-viewer).</summary>
    public string? LatestQuotePreviewUrl { get; set; }

    public IReadOnlyList<Mainflow2MessageDto> Messages { get; set; } = Array.Empty<Mainflow2MessageDto>();
    public IReadOnlyList<Mainflow2VersionDto> Versions { get; set; } = Array.Empty<Mainflow2VersionDto>();
    public IReadOnlyList<Mainflow2TimelineStepDto> Timeline { get; set; } = Array.Empty<Mainflow2TimelineStepDto>();
}

/// <summary>1 tin nhắn trong thread Mainflow 2 (lưu trong DesignLog).</summary>
public class Mainflow2MessageDto
{
    public Guid Id { get; set; }
    public Guid? AccountId { get; set; }
    public string LogType { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? AuthorName { get; set; }
    public string AuthorRole { get; set; } = string.Empty;
}

/// <summary>1 phiên bản file (GLB/STL/OBJ) gắn với DesignWork.</summary>
public class Mainflow2VersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string? Title { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public bool IsPreviewable { get; set; }
    public bool IsApproved { get; set; }
    public bool IsPrintable { get; set; }
    public DateTimeOffset Created { get; set; }
}

/// <summary>1 mốc trong timeline UI hiển thị tiến trình yêu cầu.</summary>
public class Mainflow2TimelineStepDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset? CompletedAt { get; set; }
    public bool IsDone { get; set; }
    public bool IsCurrent { get; set; }
}
