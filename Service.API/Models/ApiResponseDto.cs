namespace Service.API.Models
{
    public record ApiResponseDto<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public IEnumerable<string>? Errors { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public static ApiResponseDto<T> Ok(T data) => new() { Success = true, Data = data };
        public static ApiResponseDto<T> Fail(params string[] errors) => new() { Success = false, Errors = errors };
    }
}
