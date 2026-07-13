namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public interface ICodeGeneratorService
{
    string GenerateOrderCode(string? scope = null);
    string GenerateInvoiceCode(string? scope = null);
    string GenerateCode(string prefix, string? scope = null);
}
