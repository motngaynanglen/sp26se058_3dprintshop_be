using System.Globalization;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Common.Helpers;

public static class PaymentProcessingHelper
{
    // PayOS returns transaction times in Vietnam local time (UTC+7), with no timezone suffix.
    // We must not use DateTimeStyles.AssumeLocal because the server may run in UTC (Docker/VPS).
    private static readonly TimeZoneInfo VietnamTz = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        // Windows: "SE Asia Standard Time" | Linux/Docker: "Asia/Ho_Chi_Minh"
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
    }

    public static DateTimeOffset? ParsePayOsTransactionTime(string? transactionDateTime)
    {
        if (string.IsNullOrWhiteSpace(transactionDateTime))
            return null;

        // Formats without timezone info — treat as Vietnam local time (UTC+7).
        var formatsNoTz = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" };
        if (DateTime.TryParseExact(
                transactionDateTime,
                formatsNoTz,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localDt))
        {
            var offset = VietnamTz.GetUtcOffset(localDt);
            return new DateTimeOffset(localDt, offset);
        }

        // Formats with explicit timezone — parse as-is.
        if (DateTimeOffset.TryParse(
                transactionDateTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static string AppendNote(string? currentNote, string nextNote)
    {
        var timestamp = CoreHelper.SystemTimeNow.ToString("dd/MM/yyyy HH:mm");
        var line = $"[{timestamp}] {nextNote}";
        return string.IsNullOrWhiteSpace(currentNote) ? line : $"{currentNote}{Environment.NewLine}{line}";
    }

    public static bool IsPayOsStatus(PayOsPaymentLinkStatusResult payOsStatus, params string[] statuses)
    {
        return statuses.Any(status => string.Equals(payOsStatus.Status, status, StringComparison.OrdinalIgnoreCase));
    }

    public static long ToPayOsAmount(decimal amount)
    {
        return decimal.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
    }
}
