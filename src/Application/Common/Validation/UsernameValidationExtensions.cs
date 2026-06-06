using System.Text.RegularExpressions;

namespace sp26se058_3dprintshop_be.Application.Common.Validation;

public static class UsernameValidationExtensions
{
    private static readonly Regex UsernamePattern = new(@"^[a-zA-Z0-9@._-]+$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string> ValidUsernameFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(username => UsernamePattern.IsMatch(username))
            .WithMessage("Tên đăng nhập không được chứa dấu và khoảng trắng.");
    }
}
