using Application.Common.Interfaces;
using Domain.Common;

namespace Infrastructure.Services;

public class ApiResponseService : IApiResponseService
{
    public ApiResponse Success(string message)
    {
        return new ApiResponse { Success = true, Message = message };
    }

    public ApiResponse<T> Success<T>(T data)
    {
        return new ApiResponse<T> { Success = true, Data = data };
    }

    public ApiResponse Fail(string message)
    {
        return new ApiResponse { Success = false, Message = message };
    }

    public ApiResponse<T> Fail<T>(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }

}

