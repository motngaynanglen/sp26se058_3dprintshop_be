using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Ocsp;

namespace sp26se058_3dprintshop_be.Application.Common.Extensions;
public static class ValidationExtensions
{
    public static void AddFailure(this List<ValidationFailure> failures, string property, string message)
    {
        failures.Add(new ValidationFailure(property, message));
    }

    public static void ThrowIfAny(this List<ValidationFailure> failures)
    {
        if (failures.Any()) throw new ValidationException(failures);
    }
}
