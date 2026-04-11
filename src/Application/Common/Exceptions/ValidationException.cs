using FluentValidation.Results;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("Một hoặc nhiều lỗi đã xảy ra trong khâu kiểm tra dữ liệu đầu vào.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
