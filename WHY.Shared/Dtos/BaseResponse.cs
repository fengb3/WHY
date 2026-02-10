using System;

namespace WHY.Shared.Dtos;

/// <summary>
/// 基础响应 DTO
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class BaseResponse<T> where T : new()

{
    public T? Data { get; set; } = default;
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}
