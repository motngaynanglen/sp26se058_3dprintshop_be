namespace sp26se058_3dprintshop_be.Application.AppSupports.Queries;

public record GetSupportTypesQuery : IRequest<IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>>;

public class GetSupportTypesQueryHandler : IRequestHandler<GetSupportTypesQuery, IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>>
{
    public Task<IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>> Handle(GetSupportTypesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(SupportCatalogProvider.Types);
    }
}

public record GetSupportTypeGroupQuery(string GroupKey) : IRequest<IReadOnlyCollection<SupportCatalogItemDTO>>;

public class GetSupportTypeGroupQueryHandler : IRequestHandler<GetSupportTypeGroupQuery, IReadOnlyCollection<SupportCatalogItemDTO>>
{
    public Task<IReadOnlyCollection<SupportCatalogItemDTO>> Handle(GetSupportTypeGroupQuery request, CancellationToken cancellationToken)
    {
        if (!SupportCatalogProvider.Types.TryGetValue(request.GroupKey, out var types))
        {
            throw new DataNotFoundException(
                $"Không tìm thấy nhóm loại nghiệp vụ '{request.GroupKey}'.",
                new { AvailableGroups = SupportCatalogProvider.Types.Keys },
                useRawMessage: true);
        }

        return Task.FromResult(types);
    }
}
