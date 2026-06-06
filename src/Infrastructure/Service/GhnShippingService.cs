using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class GhnShippingService : IShippingCarrierService
{
    private readonly HttpClient _http;
    private readonly GhnSettings _settings;
    private readonly IGhnMasterDataService _ghnMasterData;
    private readonly ILogger<GhnShippingService> _logger;

    public GhnShippingService(
        HttpClient http,
        IOptions<GhnSettings> settings,
        IGhnMasterDataService ghnMasterData,
        ILogger<GhnShippingService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _ghnMasterData = ghnMasterData;
        _logger = logger;
    }

    public string CarrierCode => ShippingCarriers.Ghn;
    public string DisplayName => "Giao Hàng Nhanh (GHN)";
    public bool IsConfigured => _settings.IsConfigured;

    public async Task<ShippingQuoteDto?> GetQuoteAsync(
        ShippingQuoteContext context,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return await BuildEstimatedQuoteAsync(
                context.GhnToDistrictId ?? 0,
                context.GhnToWardCode ?? string.Empty,
                null,
                "GHN chưa cấu hình Token/ShopId — phí ước tính.",
                cancellationToken);
        }

        var toDistrict = context.GhnToDistrictId ?? _settings.DefaultToDistrictId;
        var toWard = context.GhnToWardCode ?? _settings.DefaultToWardCode;
        if (toDistrict is null or <= 0 || string.IsNullOrWhiteSpace(toWard))
        {
            return await BuildEstimatedQuoteAsync(
                0,
                string.Empty,
                null,
                "Thiếu mã quận/phường GHN — dùng phí ước tính.",
                cancellationToken);
        }

        try
        {
            var serviceId = _settings.ServiceId > 0
                ? _settings.ServiceId
                : await TryResolveServiceIdAsync(toDistrict.Value, cancellationToken);

            var payload = BuildFeePayload(
                toDistrict.Value,
                toWard,
                context.WeightGrams,
                context.OrderValue,
                serviceId);

            using var req = CreateRequest(HttpMethod.Post, "/v2/shipping-order/fee", payload);
            using var res = await _http.SendAsync(req, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GHN] Fee HTTP {Status}: {Body}", res.StatusCode, json);
                return await BuildEstimatedQuoteAsync(
                    toDistrict.Value,
                    toWard,
                    serviceId,
                    "GHN từ chối tính phí — dùng phí ước tính.",
                    cancellationToken);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : json;
                _logger.LogWarning("[GHN] Fee code != 200: {Message}", msg);
                return await BuildEstimatedQuoteAsync(
                    toDistrict.Value,
                    toWard,
                    serviceId,
                    msg ?? "Lỗi GHN",
                    cancellationToken);
            }

            if (!root.TryGetProperty("data", out var data))
                return await BuildEstimatedQuoteAsync(
                    toDistrict.Value,
                    toWard,
                    serviceId,
                    "GHN không trả data phí.",
                    cancellationToken);

            var total = data.TryGetProperty("total", out var t) ? t.GetDecimal()
                : data.TryGetProperty("service_fee", out var sf) ? sf.GetDecimal() : 0;

            if (total <= 0)
            {
                _logger.LogWarning(
                    "[GHN] Fee=0 (from {FromDistrict}/{FromWard} → to {ToDistrict}/{ToWard}, service_id={ServiceId}): {Body}",
                    _settings.FromDistrictId,
                    _settings.FromWardCode,
                    toDistrict,
                    toWard,
                    serviceId,
                    json);
                return await BuildEstimatedQuoteAsync(
                    toDistrict.Value,
                    toWard,
                    serviceId,
                    "GHN trả phí 0đ — dùng phí ước tính theo khu vực (nội thành / liên tỉnh).",
                    cancellationToken);
            }

            var leadDays = await TryGetLeadDaysAsync(toDistrict.Value, toWard, serviceId, cancellationToken)
                ?? ParseLeadDaysFromFeeData(data)
                ?? await ResolveDefaultLeadDaysAsync(toDistrict.Value, cancellationToken);

            return new ShippingQuoteDto
            {
                Carrier = CarrierCode,
                CarrierName = DisplayName,
                Fee = total,
                LeadDays = Math.Max(1, leadDays),
                IsEstimated = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GHN] Fee exception");
            return await BuildEstimatedQuoteAsync(
                toDistrict.Value,
                toWard,
                null,
                ex.Message,
                cancellationToken);
        }
    }

    public async Task<CarrierShipmentCreateResult> CreateShipmentAsync(
        CarrierShipmentCreateContext context,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(_settings.Token)) missing.Add("Token");
            if (_settings.ShopId <= 0) missing.Add("ShopId");
            if (_settings.FromDistrictId <= 0) missing.Add("FromDistrictId");
            if (string.IsNullOrWhiteSpace(_settings.FromWardCode)) missing.Add("FromWardCode");
            return CarrierShipmentCreateResult.Fail(
                $"GHN chưa cấu hình (thiếu: {string.Join(", ", missing)}). "
                + "Điền GHN trong appsettings.Development.json (dotnet run) hoặc appsettings.json / biến GHN__* (Docker). "
                + "Sau đó restart backend.");
        }

        var toDistrict = context.GhnToDistrictId ?? _settings.DefaultToDistrictId;
        var toWard = context.GhnToWardCode ?? _settings.DefaultToWardCode;
        if (toDistrict is null or <= 0 || string.IsNullOrWhiteSpace(toWard))
            return CarrierShipmentCreateResult.Fail(
                "Địa chỉ giao hàng thiếu mã quận/phường GHN — khách cần chọn Tỉnh → Quận → Phường GHN khi checkout.");

        var codAmount = context.IsInvoicePaid ? 0 : (int)Math.Round(context.OrderValue, 0);
        var paymentType = codAmount > 0 ? 2 : 1;

        var fromPhone = NormalizePhone(_settings.FromPhone ?? "0938212312");
        var fromAddress = _settings.FromAddress
            ?? "Lô E2a-7, Đường D1, Khu CNC TP.HCM, Phường Tăng Nhơn Phú A, TP. Thủ Đức, Hồ Chí Minh";
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "3D Print Shop" : _settings.FromName.Trim();

        var payload = new
        {
            payment_type_id = paymentType,
            required_note = "KHONGCHOXEMHANG",
            from_name = fromName,
            from_phone = fromPhone,
            from_address = fromAddress,
            from_ward_code = _settings.FromWardCode,
            from_district_id = _settings.FromDistrictId,
            return_phone = fromPhone,
            return_address = fromAddress,
            return_district_id = _settings.FromDistrictId,
            return_ward_code = _settings.FromWardCode,
            to_name = context.ReceiverName,
            to_phone = NormalizePhone(context.Phone),
            to_address = BuildFullAddress(context),
            to_ward_code = toWard,
            to_district_id = toDistrict.Value,
            cod_amount = codAmount,
            weight = Math.Max(1, context.WeightGrams),
            length = 10,
            width = 10,
            height = 10,
            service_type_id = _settings.ServiceTypeId,
            pick_station_id = _settings.PickStationId,
            client_order_code = context.OrderCode,
            items = context.Items.Select(i => new
            {
                name = i.Name,
                quantity = i.Quantity,
                weight = (int)Math.Max(1, i.WeightKg * 1000)
            }).ToList()
        };

        try
        {
            using var req = CreateRequest(HttpMethod.Post, "/v2/shipping-order/create", payload);
            using var res = await _http.SendAsync(req, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
                return CarrierShipmentCreateResult.Fail($"GHN HTTP {(int)res.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : json;
                var codeMsg = root.TryGetProperty("code_message", out var cm) ? cm.GetString() : null;
                if (codeMsg == "FROM_ADDRESS_CONVERT_FAIL")
                {
                    return CarrierShipmentCreateResult.Fail(
                        "GHN không map được địa chỉ kho lấy hàng (from). "
                        + "Kiểm tra GHN:FromDistrictId, FromWardCode, FromAddress trong appsettings "
                        + "và đăng ký địa chỉ lấy hàng trên portal 5sao.ghn.dev cho ShopId "
                        + $"{_settings.ShopId}. Chi tiết: {msg}");
                }

                return CarrierShipmentCreateResult.Fail(msg ?? "GHN tạo đơn thất bại");
            }

            if (!root.TryGetProperty("data", out var data))
                return CarrierShipmentCreateResult.Fail("GHN không trả data đơn.");

            var orderCode = data.TryGetProperty("order_code", out var oc) ? oc.GetString() : null;
            var sortCode = data.TryGetProperty("sort_code", out var sc) ? sc.GetString() : null;
            var status = data.TryGetProperty("status", out var st) ? st.GetString() : "ready_to_pick";

            return CarrierShipmentCreateResult.Ok(
                orderCode ?? context.OrderCode,
                sortCode ?? orderCode,
                null,
                status,
                json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GHN] Create shipment failed for {OrderCode}", context.OrderCode);
            return CarrierShipmentCreateResult.Fail(ex.Message);
        }
    }

    public bool TryMapCarrierStatus(string? carrierStatus, out string shipmentStatus)
    {
        shipmentStatus = ShipmentStatuses.Preparing;
        if (string.IsNullOrWhiteSpace(carrierStatus))
            return false;

        var s = carrierStatus.Trim().ToLowerInvariant();
        if (s is "delivered" or "delivery_success" or "success")
        {
            shipmentStatus = ShipmentStatuses.Delivered;
            return true;
        }

        if (s is "picking" or "ready_to_pick" or "storing" or "waiting_for_pick")
        {
            shipmentStatus = ShipmentStatuses.ReadyForPickup;
            return true;
        }

        if (s is "delivering" or "transporting" or "in_transit" or "picked")
        {
            shipmentStatus = ShipmentStatuses.InTransit;
            return true;
        }

        return false;
    }

    private async Task<ShippingQuoteDto> BuildEstimatedQuoteAsync(
        int toDistrictId,
        string toWardCode,
        int? serviceId,
        string? message,
        CancellationToken cancellationToken)
    {
        var isInnerCity = await IsInnerCityAsync(toDistrictId, cancellationToken);
        decimal fee;
        if (toDistrictId <= 0)
            fee = _settings.FallbackFee;
        else if (isInnerCity)
            fee = _settings.FallbackFeeInnerCity > 0 ? _settings.FallbackFeeInnerCity : _settings.FallbackFee;
        else
            fee = _settings.FallbackFeeInterProvince > 0 ? _settings.FallbackFeeInterProvince : _settings.FallbackFee;

        var leadDays = toDistrictId > 0 && !string.IsNullOrWhiteSpace(toWardCode)
            ? await TryGetLeadDaysAsync(toDistrictId, toWardCode, serviceId, cancellationToken)
            : null;
        leadDays ??= isInnerCity
            ? (_settings.FallbackLeadDaysInnerCity > 0 ? _settings.FallbackLeadDaysInnerCity : _settings.FallbackLeadDays)
            : (_settings.FallbackLeadDaysInterProvince > 0 ? _settings.FallbackLeadDaysInterProvince : _settings.FallbackLeadDays);

        return new ShippingQuoteDto
        {
            Carrier = CarrierCode,
            CarrierName = DisplayName,
            Fee = fee,
            LeadDays = Math.Max(1, leadDays.Value),
            IsEstimated = true,
            Message = message
        };
    }

    private async Task<bool> IsInnerCityAsync(int toDistrictId, CancellationToken cancellationToken)
    {
        if (toDistrictId <= 0 || _settings.FromProvinceId <= 0)
            return false;

        return await _ghnMasterData.IsDistrictInProvinceAsync(
            toDistrictId,
            _settings.FromProvinceId,
            cancellationToken);
    }

    private async Task<int> ResolveDefaultLeadDaysAsync(int toDistrictId, CancellationToken cancellationToken)
    {
        var isInnerCity = await IsInnerCityAsync(toDistrictId, cancellationToken);
        if (isInnerCity)
            return _settings.FallbackLeadDaysInnerCity > 0 ? _settings.FallbackLeadDaysInnerCity : _settings.FallbackLeadDays;

        return _settings.FallbackLeadDaysInterProvince > 0 ? _settings.FallbackLeadDaysInterProvince : _settings.FallbackLeadDays;
    }

    private object BuildFeePayload(
        int toDistrictId,
        string toWardCode,
        int weightGrams,
        decimal orderValue,
        int? serviceId)
    {
        var payload = new Dictionary<string, object?>
        {
            ["from_district_id"] = _settings.FromDistrictId,
            ["from_ward_code"] = _settings.FromWardCode,
            ["to_district_id"] = toDistrictId,
            ["to_ward_code"] = toWardCode,
            ["weight"] = Math.Max(1, weightGrams),
            ["length"] = 20,
            ["width"] = 20,
            ["height"] = 20
        };

        if (serviceId is > 0)
        {
            payload["service_id"] = serviceId.Value;
            payload["service_type_id"] = null!;
        }
        else
            payload["service_type_id"] = _settings.ServiceTypeId;

        if (orderValue > 0)
            payload["insurance_value"] = (int)Math.Min(5_000_000, Math.Round(orderValue, 0));

        return payload;
    }

    private async Task<int?> TryResolveServiceIdAsync(int toDistrictId, CancellationToken cancellationToken)
    {
        var payload = new
        {
            shop_id = _settings.ShopId,
            from_district = _settings.FromDistrictId,
            to_district = toDistrictId
        };

        try
        {
            using var req = CreateRequest(HttpMethod.Post, "/v2/shipping-order/available-services", payload);
            using var res = await _http.SendAsync(req, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogDebug("[GHN] available-services HTTP {Status}: {Body}", res.StatusCode, json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
                return null;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var service in data.EnumerateArray())
            {
                if (!service.TryGetProperty("service_type_id", out var typeEl))
                    continue;
                if (typeEl.GetInt32() != _settings.ServiceTypeId)
                    continue;
                if (service.TryGetProperty("service_id", out var idEl) && idEl.GetInt32() > 0)
                    return idEl.GetInt32();
            }

            var first = data.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object
                && first.TryGetProperty("service_id", out var fallbackId)
                && fallbackId.GetInt32() > 0)
                return fallbackId.GetInt32();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[GHN] available-services failed");
        }

        return null;
    }

    private async Task<int?> TryGetLeadDaysAsync(
        int toDistrictId,
        string toWardCode,
        int? serviceId,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["from_district_id"] = _settings.FromDistrictId,
            ["from_ward_code"] = _settings.FromWardCode,
            ["to_district_id"] = toDistrictId,
            ["to_ward_code"] = toWardCode,
            ["service_type_id"] = _settings.ServiceTypeId
        };
        if (serviceId is > 0)
            payload["service_id"] = serviceId.Value;

        try
        {
            using var req = CreateRequest(HttpMethod.Post, "/v2/shipping-order/leadtime", payload);
            using var res = await _http.SendAsync(req, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
                return null;
            if (!root.TryGetProperty("data", out var data))
                return null;

            return ParseLeadDaysFromFeeData(data);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[GHN] leadtime failed");
            return null;
        }
    }

    private static int? ParseLeadDaysFromFeeData(JsonElement data)
    {
        if (data.TryGetProperty("leadtime_order", out var order)
            && order.TryGetProperty("to_estimate_date", out var toDateEl)
            && DateTime.TryParse(toDateEl.GetString(), out var toDate))
        {
            var days = (int)Math.Ceiling((toDate.Date - DateTime.UtcNow.Date).TotalDays);
            return Math.Max(1, days);
        }

        if (data.TryGetProperty("leadtime", out var lt) && lt.ValueKind == JsonValueKind.Number)
        {
            var n = lt.GetInt32();
            // GHN đôi khi trả unix timestamp — bỏ qua nếu quá lớn.
            if (n is > 0 and < 30)
                return n;
        }

        return null;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object body)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var req = new HttpRequestMessage(method, $"{baseUrl}{path}");
        req.Headers.TryAddWithoutValidation("Token", _settings.Token.Trim());
        req.Headers.TryAddWithoutValidation("ShopId", _settings.ShopId.ToString());
        req.Content = JsonContent.Create(body);
        return req;
    }

    private static string BuildFullAddress(ShippingQuoteContext c) =>
        string.Join(", ", new[] { c.AddressLine, c.Ward, c.District, c.City }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string BuildFullAddress(CarrierShipmentCreateContext c) =>
        string.Join(", ", new[] { c.AddressLine, c.Ward, c.District, c.City }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string NormalizePhone(string phone)
    {
        var p = phone.Trim().Replace(" ", "");
        if (p.StartsWith("+84")) p = "0" + p[3..];
        if (p.StartsWith("84") && p.Length > 9) p = "0" + p[2..];
        return p;
    }
}
