using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipping.Queries;

/// <summary>[Customer/All] Báo phí vận chuyển GHN/GHTK (hoặc phí ước tính khi chưa cấu hình API).</summary>
public record GetShippingQuotesQuery : IRequest<List<ShippingQuoteDto>>
{
    /// <summary>Địa chỉ đã lưu — hoặc bỏ trống nếu gửi mã GHN trực tiếp.</summary>
    public Guid? ShippingAddressId { get; init; }

    public int WeightGrams { get; init; } = 500;

    public decimal OrderValue { get; init; }

    /// <summary>Thu hộ COD khi chưa thanh toán online.</summary>
    public bool CollectOnDelivery { get; init; }

    public int? GhnToDistrictId { get; init; }

    public string? GhnToWardCode { get; init; }
}

public class GetShippingQuotesQueryValidator : AbstractValidator<GetShippingQuotesQuery>
{
    public GetShippingQuotesQueryValidator()
    {
        RuleFor(x => x.WeightGrams).GreaterThan(0);
        RuleFor(x => x.OrderValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(x =>
                x.ShippingAddressId is { } id && id != Guid.Empty
                || (x.GhnToDistrictId is > 0 && !string.IsNullOrWhiteSpace(x.GhnToWardCode)))
            .WithMessage("Cần địa chỉ đã lưu hoặc mã quận/phường GHN.");
    }
}

public class GetShippingQuotesQueryHandler : IRequestHandler<GetShippingQuotesQuery, List<ShippingQuoteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IShippingCarrierResolver _carriers;
    private readonly IGhnAddressResolver _ghnResolver;
    private readonly IUser _user;

    public GetShippingQuotesQueryHandler(
        IApplicationDbContext context,
        IShippingCarrierResolver carriers,
        IGhnAddressResolver ghnResolver,
        IUser user)
    {
        _context = context;
        _carriers = carriers;
        _ghnResolver = ghnResolver;
        _user = user;
    }

    public async Task<List<ShippingQuoteDto>> Handle(
        GetShippingQuotesQuery request,
        CancellationToken cancellationToken)
    {
        ShippingAddress? address = null;
        if (request.ShippingAddressId is { } addrId && addrId != Guid.Empty)
        {
            address = await _context.ShippingAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == addrId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy địa chỉ giao hàng.");

            if (!string.IsNullOrWhiteSpace(_user.Id))
            {
                var accountId = _user.Id.ToGuid();
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken);
                if (customer != null && address.CustomerId != customer.Id)
                    throw new UnauthorizedAccessException("Địa chỉ không thuộc về bạn.");
            }
        }

        var toDistrict = request.GhnToDistrictId ?? address?.GhnDistrictId;
        var toWard = request.GhnToWardCode ?? address?.GhnWardCode;

        if ((toDistrict is null or <= 0 || string.IsNullOrWhiteSpace(toWard)) && address != null)
        {
            var resolved = await _ghnResolver.TryResolveAsync(
                new GhnAddressResolveInput(address.Ward, address.District, address.City, address.Province),
                cancellationToken);
            if (resolved != null)
            {
                toDistrict = resolved.DistrictId;
                toWard = resolved.WardCode;
            }
        }

        var ctx = new ShippingQuoteContext(
            address?.ReceiverName ?? "Khách",
            address?.Phone ?? "",
            address?.AddressLine ?? "",
            address?.Ward ?? "",
            address?.District ?? "",
            address?.City ?? "",
            address?.Province ?? "Việt Nam",
            request.WeightGrams,
            request.OrderValue,
            request.CollectOnDelivery,
            toDistrict,
            toWard);

        var quotes = new List<ShippingQuoteDto>();
        foreach (var service in _carriers.GetAll())
        {
            var quote = await service.GetQuoteAsync(ctx, cancellationToken);
            if (quote != null)
                quotes.Add(quote);
        }

        return quotes.OrderBy(q => q.Fee).ToList();
    }
}
