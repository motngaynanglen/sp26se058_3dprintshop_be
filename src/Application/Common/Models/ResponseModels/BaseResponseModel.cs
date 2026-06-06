using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
public class BaseResponseModel<T>
{
    public int StatusCode { get; set; }
    public string Code { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public object? AdditionalData { get; set; }

    public BaseResponseModel(int statusCode, string code, T? data, object? additionalData = null, string? message = null)
    {
        this.StatusCode = statusCode;
        this.Code = code;
        this.Data = data;
        this.AdditionalData = additionalData;
        this.Message = message;
    }

    public BaseResponseModel(int statusCode, string code, string? message)
    {
        this.StatusCode = statusCode;
        this.Code = code;
        this.Message = message;
    }

    public static BaseResponseModel<T> OkResponseModel(T data, object? additionalData = null, string? message = null, string code = ResponseCodeConstants.SUCCESS)
    {
        return new BaseResponseModel<T>(200, code, data, additionalData,message);
    }

    public static BaseResponseModel<T> NotFoundResponseModel(T? data, object? additionalData = null, string code = ResponseCodeConstants.NOT_FOUND)
    {
        return new BaseResponseModel<T>(404, code, data, additionalData);
    }

    public static BaseResponseModel<T> BadRequestResponseModel(T? data, object? additionalData = null, string? message = null, string code = ResponseCodeConstants.FAILED)
    {
        return new BaseResponseModel<T>(400, code, data, additionalData, message);
    }

    public static BaseResponseModel<T> InternalErrorResponseModel(T? data, object? additionalData = null, string code = ResponseCodeConstants.FAILED)
    {
        return new BaseResponseModel<T>(500, code, data, additionalData);
    }
}

public class BaseResponseModel : BaseResponseModel<object>
{
    public BaseResponseModel(int statusCode, string code, object? data, object? additionalData = null, string? message = null) : base(statusCode, code, data, additionalData, message)
    {
    }

    public BaseResponseModel(int statusCode, string code, string? message) : base(statusCode, code, message)
    {
    }
}
