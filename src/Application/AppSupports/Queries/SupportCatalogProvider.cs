using sp26se058_3dprintshop_be.Domain.Common;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.AppSupports.Queries;

internal static class SupportCatalogProvider
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>> Statuses =
        new Dictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>(StringComparer.OrdinalIgnoreCase)
        {
            ["design-work"] = Map(DesignWorkStatus.All),
            ["invoice"] = Map(InvoiceStatuses.All),
            ["order-item"] = Map(OrderItemStatuses.All),
            ["order"] = Map(OrderStatuses.All),
            ["shipment"] = Map(ShipmentStatuses.All),
            ["shipment-address-change-request"] = Map(ShipmentAddressChangeRequestStatuses.All),
            ["transaction"] = Map(TransactionStatuses.All),
            ["adjustment-request"] = Map(AdjustmentRequestStatuses.All),
        };

    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>> Types =
        new Dictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>(StringComparer.OrdinalIgnoreCase)
        {
            ["design-log"] = Map(DesignLogType.All),
            ["design-relationship"] = Map(DesignRelationshipType.All),
            ["inventory-transaction"] = Map(InventoryTransactionTypes.All),
            ["payment-method"] = Map(PaymentMethods.All),
            ["service-option-selection"] = Map(ServiceOptionSelectionTypes.All),
            ["source"] = Map(SourceTypes.All),
        };

    public static readonly IReadOnlyCollection<SupportCatalogItemDTO> Roles =
    [
        new(Domain.Constants.Roles.ADMIN, "Quản trị hệ thống", "Tài khoản system admin cấu hình bằng JSON, chỉ dùng cho quản lý tài khoản.", []),
        new(Domain.Constants.Roles.MANAGER, "Quản lý", "Quản lý vận hành và quản lý tài khoản nhân viên.", []),
        new(Domain.Constants.Roles.STAFF, "Nhân viên", "Xử lý thiết kế, đơn hàng, bản nháp kỹ thuật và hỗ trợ khách hàng.", []),
        new(Domain.Constants.Roles.CUSTOMER, "Khách hàng", "Tạo yêu cầu thiết kế/in ấn, đặt hàng và theo dõi tiến độ.", []),
        new(Domain.Constants.Roles.GUEST, "Khách vãng lai", "Người dùng chưa đăng nhập hoặc chưa được gán vai trò nghiệp vụ.", []),
    ];

    public static SupportCatalogDTO GetCatalog() => new(Statuses, Types, Roles);

    private static IReadOnlyCollection<SupportCatalogItemDTO> Map(IEnumerable<StatusDefinition> definitions)
    {
        return definitions
            .Select(x => new SupportCatalogItemDTO(
                x.Value,
                x.Label,
                x.Description,
                x.AllowedNextStatuses ?? []))
            .ToList();
    }
}
