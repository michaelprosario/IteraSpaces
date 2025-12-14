using System.Collections.Generic;

namespace AppCore.Common
{
    public class AppResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<ValidationError>? ValidationErrors { get; set; }
        public string? ErrorCode { get; set; }

        public static AppResult<T> SuccessResult(T data, string? message = null)
        {
            return new AppResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static AppResult<T> FailureResult(string message, string? errorCode = null)
        {
            return new AppResult<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                ValidationErrors = new List<ValidationError>()
            };
        }

        public static AppResult<T> ValidationFailure(List<ValidationError> errors)
        {
            return new AppResult<T>
            {
                Success = false,
                Message = "Validation failed",
                ValidationErrors = errors
            };
        }
    }

    public class ValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
