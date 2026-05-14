using System.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;
using sp26se058_3dprintshop_be.Application.Common.Attributes;

public class ConstantSchemaProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        // Fix image_25e506: Dùng ContextualType thay vì Type
        var properties = context.ContextualType.Properties;

        foreach (var prop in properties)
        {
            // Lấy Attribute từ PropertyInfo của ContextualType
            var attribute = prop.PropertyInfo?.GetCustomAttribute<SwaggerConstantAttribute>();
            if (attribute == null) continue;

            // Tìm schema của property tương ứng
            if (context.Schema.Properties.TryGetValue(prop.Name, out var propertySchema))
            {
                var values = GetConstants(attribute.ConstantType);

                if (values.Any())
                {
                    propertySchema.Enumeration.Clear();
                    foreach (var value in values)
                    {
                        propertySchema.Enumeration.Add(value);
                    }

                    // Không gán trực tiếp Type = "string" vì nó read-only
                    // NSwag v14 tự hiểu type dựa trên property, ta chỉ cần nạp Enum
                    propertySchema.Description = (propertySchema.Description ?? "") +
                        $" (Allowed values: {string.Join(", ", values)})";
                }
            }
        }
    }

    private List<string> GetConstants(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                   .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                   // IsLiteral xác định nó là 'const'
                   // !f.IsInitOnly để loại bỏ các trường 'readonly' như cái List All của Bách
                   .Select(f => f.GetRawConstantValue()?.ToString() ?? "")
                   .Where(v => !string.IsNullOrEmpty(v))
                   .ToList();
    }
}
