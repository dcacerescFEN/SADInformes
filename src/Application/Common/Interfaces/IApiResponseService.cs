using Domain.Common;

namespace Application.Common.Interfaces;

public interface IApiResponseService
{
    ApiResponse Success(string message);
    ApiResponse<T> Success<T>(T data);
    ApiResponse Fail(string message);
    ApiResponse<T> Fail<T>(string message);
}
