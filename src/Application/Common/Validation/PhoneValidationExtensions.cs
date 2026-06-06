using System.Text.RegularExpressions;

namespace sp26se058_3dprintshop_be.Application.Common.Validation;

public static class PhoneValidationExtensions
{
    private static readonly Regex VietnamesePhonePattern = new(@"^0[0-9]{9,10}$", RegexOptions.Compiled);

    public static string NormalizeVietnamesePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var p = phone.Trim().Replace(" ", string.Empty);
        if (p.StartsWith("+84", StringComparison.Ordinal))
        {
            p = "0" + p[3..];
        }
        else if (p.StartsWith("84", StringComparison.Ordinal) && p.Length > 9)
        {
            p = "0" + p[2..];
        }

        return Regex.Replace(p, @"\D", string.Empty);
    }

    public static bool IsValidVietnamesePhone(string? phone) =>
        VietnamesePhonePattern.IsMatch(NormalizeVietnamesePhone(phone));

    public static IRuleBuilderOptions<T, string?> ValidVietnamesePhone<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(phone => string.IsNullOrWhiteSpace(phone) || IsValidVietnamesePhone(phone))
            .WithMessage("Số điện thoại không hợp lệ. Vui lòng nhập 10–11 chữ số, bắt đầu bằng 0.");
    }
}
