using FluentValidation.Results;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;

public abstract class BaseValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; protected set; }

    protected BaseValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    protected BaseValidationException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}

