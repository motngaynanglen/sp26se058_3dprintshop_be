using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Attributes;
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)] 
public class SwaggerConstantAttribute : Attribute
{
    public Type ConstantType { get; }

    public SwaggerConstantAttribute(Type constantType)
    {
        ConstantType = constantType;
    }
}
