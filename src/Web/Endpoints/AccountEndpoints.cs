using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sp26se058_3dprintshop_be.Web.Endpoints;
public class AccountEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account")
                       .WithTags("Account")
                       //.RequireAuthorization(Roles.ADMIN)
                       .WithOpenApi(); // Ép Swagger phải nhận diện group này

        group.MapPost("/query", QueryAccounts);
        group.MapPost("/add", CreateAccount);
        group.MapGet("/detail/{id}", GetAccountDetail);
        group.MapPut("/update/{id}", UpdateAccount);

    }
    public async Task<IResult> CreateAccount([FromServices] ISender sender,[FromBody] CreateAccountCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    data: result,
                    message: "Tạo tài khoản thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Tạo tài khoản thất bại"
            ));
        }
    }
    public async Task<IResult> QueryAccounts([FromServices] ISender sender, [FromBody] GetAccountsWithPaginationQuery command)
    {
        try
        {
            var result = await sender.Send(command);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<AccountDto>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));
        }
        catch (UnauthorizedAccessException)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(null, code: ResponseCodeConstants.INVALID_CREDENTIALS),
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }
    public async Task<IResult> GetAccountDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetAccountDetailQuery { Id = id });
            return TypedResults.Ok(BaseResponseModel<AccountDto>.OkResponseModel(
                    data: result,
                    message: "Lấy thông tin chi tiết thành công",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception)
        {
            return TypedResults.NotFound(BaseResponseModel<object>.NotFoundResponseModel(null));
        }
    }
    public async Task<IResult> UpdateAccount([FromServices] ISender sender,[FromRoute] Guid id, [FromBody] UpdateAccountCommand command)
    {
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                data: new { id = result },
                message: "Cập nhật thành công",
                code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Lỗi trong quá trình cập nhật"
            ));
        }
    }
}
