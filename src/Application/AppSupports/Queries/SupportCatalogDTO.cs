namespace sp26se058_3dprintshop_be.Application.AppSupports.Queries;

public record SupportCatalogItemDTO(
    string Value,
    string Label,
    string? Description,
    IReadOnlyCollection<string> AllowedNextValues);

public record SupportCatalogDTO(
    IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>> Statuses,
    IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>> Types,
    IReadOnlyCollection<SupportCatalogItemDTO> Roles);
