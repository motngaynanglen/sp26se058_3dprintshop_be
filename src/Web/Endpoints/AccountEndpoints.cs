using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.Accounts.Queries;
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
                       .WithOpenApi();

        group.MapPost("/query", QueryAccounts)
            .WithSummary("[Admin/Manager] Truy vấn danh sách tài khoản.")
            .WithDescription("Quản trị hệ thống xem toàn bộ tài khoản. Quản lý chỉ xem và quản lý tài khoản nhân viên.");
        group.MapPost("/basic-query", QueryBasicUsers)
            .WithSummary("[Staff/Manager] Tìm kiếm thông tin cơ bản người dùng.")
            .WithDescription("Dùng cho nghiệp vụ: tìm customer/staff/manager theo tên, email, username hoặc số điện thoại. Không trả audit/deleted data.");
        group.MapPost("/add", CreateAccount)
            .WithSummary("[Admin/Manager] Tạo tài khoản mới.")
            .WithDescription("Quản trị hệ thống tạo tài khoản quản lý/nhân viên/khách hàng. Quản lý chỉ được tạo tài khoản nhân viên.");
        group.MapGet("/{id}/detail", GetAccountDetail)
            .WithSummary("[Admin/Manager] Lấy thông tin chi tiết tài khoản.")
            .WithDescription("Quản trị hệ thống xem toàn bộ. Quản lý chỉ xem chi tiết tài khoản nhân viên.");
        group.MapGet("/{id}/basic", GetBasicUserDetail)
            .WithSummary("[Staff/Manager] Xem thông tin cơ bản người dùng.")
            .WithDescription("Trả về thông tin liên hệ cơ bản để hỗ trợ xử lý công việc.");

        group.MapPatch("/{id}/update", UpdateAccount)
            .WithSummary("[Admin/Manager] Cập nhật thông tin tài khoản với mã. Quản lý chỉ cập nhật nhân viên.");

        group.MapPatch("/{id}/active", Active)
            .WithSummary("[Admin/Manager] Kích hoạt tài khoản có mã. Quản lý chỉ kích hoạt nhân viên.")
            .WithDescription("Để trống dữ liệu Request BODY khi gọi");
        group.MapPatch("/{id}/deactive", Deactive)
            .WithSummary("[Admin/Manager] Tạm ngưng hoạt động tài khoản có mã. Quản lý chỉ vô hiệu hóa nhân viên.")
            .WithDescription("Để trống dữ liệu Request BODY khi gọi");
        group.MapDelete("/{id}/delete", Delete)
            .WithSummary("[Admin/Manager] Xóa mềm tài khoản có mã. Quản lý chỉ xóa mềm nhân viên.")
            .WithDescription("Để trống dữ liệu Request BODY khi gọi");

        group.MapGet("/me", GetAccountMine)
           .WithSummary("[Customer] Lấy thông tin chi tiết của tài khoản hiển tại.");
        group.MapPatch("/me/update", UpdateAccountMine)
           .WithSummary("[Customer] Cập nhật thông tin chi tiết của tài khoản hiển tại.");
        group.MapPatch("/change-password", ChangePassword)
            .WithSummary("[Customer] Đổi mật khẩu của tài khoản hiện tại.");


    }
    public async Task<IResult> CreateAccount([FromServices] ISender sender, [FromBody] CreateAccountCommand command)
    {

        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                data: result,
                message: "Tạo tài khoản thành công!",
                code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> QueryAccounts([FromServices] ISender sender, [FromBody] GetAccountsWithPaginationQuery command)
    {

        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<IEnumerable<AccountDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                successMessage: "Lấy danh sách tài khoản thành công.",
                emptyMessage: "Không tìm thấy tài khoản nào."
            ));

    }
    public async Task<IResult> QueryBasicUsers([FromServices] ISender sender, [FromBody] GetUsersBasicQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(BaseResponseModel<IEnumerable<UserBasicDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                successMessage: "Lấy danh sách người dùng thành công.",
                emptyMessage: "Không tìm thấy người dùng nào."
            ));
    }
    public async Task<IResult> GetAccountDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {

        var result = await sender.Send(new GetAccountDetailQuery { Id = id });
        return TypedResults.Ok(BaseResponseModel<AccountDTO>.OkResponseModel(
                data: result,
                message: "Lấy thông tin chi tiết thành công",
                code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> GetBasicUserDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new GetUserBasicDetailQuery(id));
        return TypedResults.Ok(BaseResponseModel<UserBasicDTO>.OkResponseModel(
                data: result,
                message: "Lấy thông tin cơ bản thành công",
                code: ResponseCodeConstants.SUCCESS
            ));
    }
    public async Task<IResult> GetAccountMine([FromServices] ISender sender)
    {

        var result = await sender.Send(new GetAccountMineDetailQuery());
        return TypedResults.Ok(BaseResponseModel<AccountDTO>.OkResponseModel(
                data: result,
                message: "Lấy thông tin chi tiết thành công",
                code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> UpdateAccount([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateAccountCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: new { id = result },
            message: "Cập nhật thành công",
            code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> UpdateAccountMine([FromServices] ISender sender, [FromBody] UpdateAccountMineCommand command)
    {

        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: new { id = result },
            message: "Cập nhật thành công",
            code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> ChangePassword([FromServices] ISender sender, [FromBody] ChangePasswordAccountCommand command)
    {

        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
            data: true,
            message: "Cập nhật thành công",
            code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> Deactive([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] DeactiveAccountCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Cập nhật thành công",
            code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> Active([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] ActiveAccountCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Cập nhật thành công",
            code: ResponseCodeConstants.SUCCESS
            ));

    }
    public async Task<IResult> Delete([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] DeleteAccountCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Cập nhật thành công",
            code: ResponseCodeConstants.SUCCESS
            ));

    }
}
