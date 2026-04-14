using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Common;
public record StatusDefinition(
    string Value,
    string Label,
    string? Description = null,
    string[]? AllowedNextStatuses = null
    );
