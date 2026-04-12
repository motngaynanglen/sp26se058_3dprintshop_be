using FluentValidation.Results;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("Dữ liệu đầu vào không hợp lệ.")
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
