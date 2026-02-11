using System;

namespace WHY.Shared.Dtos;

/// <summary>
/// 基础响应 DTO
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class BaseResponse<T>
    where T : new()
{
    public T? Data { get; set; } = default;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
}

public static class BaseResponse
{
    public static void ThrowIfError<T>(this BaseResponse<T> response) where T : new()
    {
        if (response.StatusCode >= 400)
        {
            throw new InvalidOperationException($"API Error: {response.Message} (Status Code: {response.StatusCode})");
        }
    }
}
