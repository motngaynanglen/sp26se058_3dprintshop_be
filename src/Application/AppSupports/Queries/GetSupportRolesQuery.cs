namespace sp26se058_3dprintshop_be.Application.AppSupports.Queries;

public record GetSupportRolesQuery : IRequest<IReadOnlyCollection<SupportCatalogItemDTO>>;

public class GetSupportRolesQueryHandler : IRequestHandler<GetSupportRolesQuery, IReadOnlyCollection<SupportCatalogItemDTO>>
{
    public Task<IReadOnlyCollection<SupportCatalogItemDTO>> Handle(GetSupportRolesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(SupportCatalogProvider.Roles);
    }
}
