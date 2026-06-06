using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class GhtkShippingService : IShippingCarrierService
{
    private readonly HttpClient _http;
    private readonly GhtkSettings _settings;
    private readonly ILogger<GhtkShippingService> _logger;

    public GhtkShippingService(
        HttpClient http,
        IOptions<GhtkSettings> settings,
        ILogger<GhtkShippingService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public string CarrierCode => ShippingCarriers.Ghtk;
    public string DisplayName => "Giao Hàng Tiết Kiệm (GHTK)";
    public bool IsConfigured => _settings.IsConfigured;

    public async Task<ShippingQuoteDto?> GetQuoteAsync(
        ShippingQuoteContext context,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return new ShippingQuoteDto
            {
                Carrier = CarrierCode,
                CarrierName = DisplayName,
                Fee = _settings.FallbackFee,
                LeadDays = _settings.FallbackLeadDays,
                IsEstimated = true,
                Message = "GHTK chưa cấu hình Token/điểm lấy hàng — phí ước tính."
            };
        }

        var query = new Dictionary<string, string?>
        {
            ["address"] = BuildCustomerAddress(context),
            ["province"] = PickProvince(context),
            ["district"] = context.District,
            ["ward"] = context.Ward,
            ["pick_address"] = _settings.PickAddress,
            ["pick_province"] = _settings.PickProvince,
            ["pick_district"] = _settings.PickDistrict,
            ["pick_ward"] = _settings.PickWard,
            ["weight"] = Math.Max(1, context.WeightGrams).ToString(),
            ["value"] = Math.Max(0, (int)Math.Round(context.OrderValue, 0)).ToString(),
            ["transport"] = "road"
        };

        try
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/services/shipment/fee?{BuildQueryString(query)}";
            using var req = CreateRequest(HttpMethod.Get, url);
            using var res = await _http.SendAsync(req, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GHTK] Fee HTTP {Status}: {Body}", res.StatusCode, json);
                return EstimatedQuote("GHTK từ chối tính phí — dùng phí ước tính.");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean())
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : json;
                return EstimatedQuote(msg);
            }

            if (!root.TryGetProperty("fee", out var feeObj))
                return EstimatedQuote("GHTK không trả fee.");

            var fee = feeObj.TryGetProperty("fee", out var f) ? f.GetDecimal()
                : feeObj.TryGetProperty("ship_fee", out var sf) ? sf.GetDecimal() : _settings.FallbackFee;

            return new ShippingQuoteDto
            {
                Carrier = CarrierCode,
                CarrierName = DisplayName,
                Fee = fee,
                LeadDays = _settings.FallbackLeadDays,
                IsEstimated = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GHTK] Fee exception");
            return EstimatedQuote(ex.Message);
        }
    }

    public async Task<CarrierShipmentCreateResult> CreateShipmentAsync(
        CarrierShipmentCreateContext context,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return CarrierShipmentCreateResult.Fail(
                "GHTK chưa cấu hình. Điền GHTK:Token và thông tin điểm lấy hàng (Pick*) trong appsettings.");

        var pickMoney = context.IsInvoicePaid ? 0 : (int)Math.Round(context.OrderValue, 0);
        var products = context.Items.Select(i => new
        {
            name = i.Name,
            weight = Math.Max(0.1, (double)i.WeightKg),
            quantity = i.Quantity,
            product_code = i.Name.Length > 50 ? i.Name[..50] : i.Name
        }).ToList();

        var order = new
        {
            id = context.OrderCode,
            pick_name = _settings.PickName,
            pick_address = _settings.PickAddress,
            pick_province = _settings.PickProvince,
            pick_district = _settings.PickDistrict,
            pick_ward = _settings.PickWard,
            pick_tel = _settings.PickTel,
            name = context.ReceiverName,
            address = BuildCustomerAddress(context),
            province = PickProvince(context),
            district = context.District,
            ward = context.Ward,
            street = context.AddressLine,
            tel = NormalizePhone(context.Phone),
            is_freeship = pickMoney > 0 ? 0 : 1,
            pick_date = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd"),
            pick_money = pickMoney,
            value = (int)Math.Round(context.OrderValue, 0),
            transport = "road",
            pick_option = "cod"
        };

        var payload = new { products, order };

        try
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/services/shipment/order?ver=1.5";
            using var req = CreateRequest(HttpMethod.Post, url);
            req.Content = JsonContent.Create(payload);
            using var res = await _http.SendAsync(req, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode)
                return CarrierShipmentCreateResult.Fail($"GHTK HTTP {(int)res.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean())
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : json;
                return CarrierShipmentCreateResult.Fail(msg ?? "GHTK tạo đơn thất bại");
            }

            if (!root.TryGetProperty("order", out var orderEl))
                return CarrierShipmentCreateResult.Fail("GHTK không trả order.");

            var label = orderEl.TryGetProperty("label", out var l) ? l.GetString() : null;
            var tracking = orderEl.TryGetProperty("tracking_id", out var t) ? t.GetInt64().ToString() : label;
            var partnerId = orderEl.TryGetProperty("partner_id", out var p) ? p.GetString() : context.OrderCode;
            var status = orderEl.TryGetProperty("status", out var st) ? st.GetString() : null;

            return CarrierShipmentCreateResult.Ok(
                partnerId ?? context.OrderCode,
                tracking ?? label,
                null,
                status,
                json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GHTK] Create shipment failed for {OrderCode}", context.OrderCode);
            return CarrierShipmentCreateResult.Fail(ex.Message);
        }
    }

    public bool TryMapCarrierStatus(string? carrierStatus, out string shipmentStatus)
    {
        shipmentStatus = ShipmentStatuses.Preparing;
        if (string.IsNullOrWhiteSpace(carrierStatus))
            return false;

        if (int.TryParse(carrierStatus.Trim(), out var statusId))
        {
            shipmentStatus = statusId switch
            {
                >= 5 and <= 6 => ShipmentStatuses.Delivered,
                4 => ShipmentStatuses.InTransit,
                2 or 3 => ShipmentStatuses.InTransit,
                1 or 12 => ShipmentStatuses.ReadyForPickup,
                _ => ShipmentStatuses.Preparing
            };
            return true;
        }

        var s = carrierStatus.Trim().ToLowerInvariant();
        if (s.Contains("deliver") && s.Contains("ed"))
        {
            shipmentStatus = ShipmentStatuses.Delivered;
            return true;
        }

        if (s.Contains("cancel") || s.Contains("return"))
            return false;

        if (s.Contains("pick") || s.Contains("recei"))
        {
            shipmentStatus = ShipmentStatuses.ReadyForPickup;
            return true;
        }

        if (s.Contains("transit") || s.Contains("delivering"))
        {
            shipmentStatus = ShipmentStatuses.InTransit;
            return true;
        }

        return false;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("Token", _settings.Token.Trim());
        req.Headers.TryAddWithoutValidation("X-Client-Source", _settings.PartnerCode);
        return req;
    }

    private ShippingQuoteDto EstimatedQuote(string? message) =>
        new()
        {
            Carrier = CarrierCode,
            CarrierName = DisplayName,
            Fee = _settings.FallbackFee,
            LeadDays = _settings.FallbackLeadDays,
            IsEstimated = true,
            Message = message
        };

    private static string PickProvince(ShippingQuoteContext c) =>
        string.IsNullOrWhiteSpace(c.Province) ? c.City : c.Province;

    private static string PickProvince(CarrierShipmentCreateContext c) =>
        string.IsNullOrWhiteSpace(c.Province) ? c.City : c.Province;

    private static string BuildCustomerAddress(ShippingQuoteContext c) =>
        string.Join(", ", new[] { c.AddressLine, c.Ward, c.District, c.City }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string BuildCustomerAddress(CarrierShipmentCreateContext c) =>
        string.Join(", ", new[] { c.AddressLine, c.Ward, c.District, c.City }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string NormalizePhone(string phone)
    {
        var p = phone.Trim().Replace(" ", "");
        if (p.StartsWith("+84")) p = "0" + p[3..];
        if (p.StartsWith("84") && p.Length > 9) p = "0" + p[2..];
        return p;
    }

    private static string BuildQueryString(Dictionary<string, string?> query) =>
        string.Join("&", query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
}
