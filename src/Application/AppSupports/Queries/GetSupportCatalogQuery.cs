namespace sp26se058_3dprintshop_be.Application.AppSupports.Queries;

public record GetSupportCatalogQuery : IRequest<SupportCatalogDTO>;

public class GetSupportCatalogQueryHandler : IRequestHandler<GetSupportCatalogQuery, SupportCatalogDTO>
{
    public Task<SupportCatalogDTO> Handle(GetSupportCatalogQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(SupportCatalogProvider.GetCatalog());
    }
}
