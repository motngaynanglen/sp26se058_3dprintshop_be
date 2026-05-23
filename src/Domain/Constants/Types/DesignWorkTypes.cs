namespace sp26se058_3dprintshop_be.Domain.Constants.Types;

/// <summary>
/// Values for DesignWork.WorkType.
/// Distinguishes Design Service (staff designs for customer) from
/// POD/Quick Print (customer provides their own print-ready file).
/// null = legacy data created before WorkType was introduced (treated as DesignService).
/// </summary>
public static class DesignWorkTypes
{
    public const string DesignService = "DESIGN_SERVICE";
    public const string PrintService  = "PRINT_SERVICE";

    public static bool IsValid(string? s) => s is null or DesignService or PrintService;
}
