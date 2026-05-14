using System.Reflection;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using sp26se058_3dprintshop_be.Application.Common.Attributes;
using Namotion.Reflection;
using NSwag;

public class ConstantOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var methodInfo = context.MethodInfo;
        // Duyệt qua tất cả tham số của Operation (Endpoint)
        foreach (var parameter in context.OperationDescription.Operation.Parameters)
        {
            /*// Tìm trong danh sách Parameters của Context để lấy Metadata gốc
            var paramMetadata = context.Parameters.FirstOrDefault(p => p.Value.Name == parameter.Name).Key;

            // Nếu không tìm thấy metadata trực tiếp (thường gặp khi dùng AsParameters)
            // ta phải dựa vào tên để map ngược lại hoặc dùng ContextualType
            if (paramMetadata == null) continue;

            var attribute = paramMetadata.GetCustomAttribute<SwaggerConstantAttribute>();*/

            var attribute = methodInfo.GetParameters() // Hàm chuẩn của System.Reflection
                .SelectMany(p => p.ParameterType.GetProperties()) // Lấy các thuộc tính của Class DTO
                .Where(prop => string.Equals(prop.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))
                .Select(prop => prop.GetCustomAttribute<SwaggerConstantAttribute>())
                .FirstOrDefault(a => a != null);

            if (attribute != null)
            {
                var values = GetConstants(attribute.ConstantType);
                //if (values.Any())
                //{
                //    // Ép kiểu cho Schema của tham số đó
                //    parameter.ActualSchema.Enumeration.Clear();
                //    foreach (var v in values)
                //    {
                //        parameter.ActualSchema.Enumeration.Add(v);
                //    }
                //}
                // Nếu là List<string>, ActualSchema.Type sẽ là Array và các item nằm trong ActualSchema.Item
                var targetSchema = (parameter.ActualSchema.Type == NJsonSchema.JsonObjectType.Array && parameter.ActualSchema.Item != null)
                                   ? parameter.ActualSchema.Item
                                   : parameter.ActualSchema;

                // Xóa các giá trị cũ và nạp hằng số mới
                targetSchema.Enumeration.Clear();
                foreach (var v in values)
                {
                    targetSchema.Enumeration.Add(v);
                }

                // Nếu là Array, gợi ý Swagger UI hiển thị kiểu chọn nhiều
                if (parameter.ActualSchema.Type == NJsonSchema.JsonObjectType.Array)
                {
                    parameter.Style = OpenApiParameterStyle.Form;
                    parameter.Explode = true;
                }
            }
        }
        return true;
    }

    //private List<string> GetConstants(Type type) => type
    //    .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
    //    .Where(f => f.IsLiteral && f.FieldType == typeof(string))
    //    .Select(f => f.GetRawConstantValue()?.ToString() ?? "")
    //    .ToList();
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
