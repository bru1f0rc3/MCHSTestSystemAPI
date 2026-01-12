namespace MCHSWebAPI.Common;

/// <summary>
/// Результат операции с возможностью возврата данных или ошибки
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; }
    public string? Error { get; }
    public List<string>? Errors { get; }

    private Result(bool isSuccess, T? data, string? error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        Errors = errors;
    }

    public static Result<T> Success(T data) => new(true, data, null);

    public static Result<T> Failure(string error) => new(false, default, error);

    public static Result<T> Failure(List<string> errors) =>
        new(false, default, errors.FirstOrDefault(), errors);

    public static Result<T> Failure(string error, List<string> errors) =>
        new(false, default, error, errors);
}

/// <summary>
/// Результат операции без возвращаемых данных
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public List<string>? Errors { get; }

    private Result(bool isSuccess, string? error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public static Result Failure(List<string> errors) =>
        new(false, errors.FirstOrDefault(), errors);
}
