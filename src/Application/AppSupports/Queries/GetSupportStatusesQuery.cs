namespace sp26se058_3dprintshop_be.Application.AppSupports.Queries;

public record GetSupportStatusesQuery : IRequest<IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>>;

public class GetSupportStatusesQueryHandler : IRequestHandler<GetSupportStatusesQuery, IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>>
{
    public Task<IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>> Handle(GetSupportStatusesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(SupportCatalogProvider.Statuses);
    }
}

public record GetSupportStatusGroupQuery(string GroupKey) : IRequest<IReadOnlyCollection<SupportCatalogItemDTO>>;

public class GetSupportStatusGroupQueryHandler : IRequestHandler<GetSupportStatusGroupQuery, IReadOnlyCollection<SupportCatalogItemDTO>>
{
    public Task<IReadOnlyCollection<SupportCatalogItemDTO>> Handle(GetSupportStatusGroupQuery request, CancellationToken cancellationToken)
    {
        if (!SupportCatalogProvider.Statuses.TryGetValue(request.GroupKey, out var statuses))
        {
            throw new DataNotFoundException(
                $"Không tìm thấy nhóm trạng thái '{request.GroupKey}'.",
                new { AvailableGroups = SupportCatalogProvider.Statuses.Keys },
                useRawMessage: true);
        }

        return Task.FromResult(statuses);
    }
}
