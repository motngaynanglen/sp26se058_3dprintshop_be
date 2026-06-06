using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Infrastructure.Data;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DevEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev")
            .WithTags("Dev / Smoke")
            .WithOpenApi();

        group.MapPost("/seed-mock", SeedMockData)
            .WithSummary("[Dev] Seed mock data (idempotent) — Materials, Concepts, DesignTemplates, Variants, demo accounts, orders, feedback.")
            .WithDescription("Tài khoản demo (mật khẩu mặc định Pass@123): manager01, staff01, staff02, customer01..03. Có thể chạy nhiều lần — bỏ qua entity đã tồn tại.");
    }

    private static async Task<IResult> SeedMockData(
        [FromServices] MockDataSeeder seeder,
        CancellationToken ct)
    {
        try
        {
            var result = await seeder.SeedAsync(ct);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: new
                {
                    summary = result.ToString(),
                    accountsCreated = result.AccountsCreated,
                    materialsCreated = result.MaterialsCreated,
                    conceptTagsCreated = result.ConceptTagsCreated,
                    designTemplatesCreated = result.DesignTemplatesCreated,
                    designVariantsCreated = result.DesignVariantsCreated,
                    shippingAddressesCreated = result.ShippingAddressesCreated,
                    ordersCreated = result.OrdersCreated,
                    feedbacksCreated = result.FeedbacksCreated,
                    demoCredentials = new
                    {
                        password = result.PasswordForDemoUsers,
                        accounts = new[]
                        {
                            "manager01 / Manager",
                            "staff01 / Staff",
                            "staff02 / Staff",
                            "customer01 / Customer (có sẵn đơn hàng demo + feedback)",
                            "customer02 / Customer",
                            "customer03 / Customer",
                        }
                    }
                },
                message: "Seed mock data thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                new BaseResponseModel<object>(
                    statusCode: 500,
                    code: ResponseCodeConstants.FAILED,
                    data: null,
                    additionalData: null,
                    message: $"Seed thất bại: {ex.Message}"),
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
