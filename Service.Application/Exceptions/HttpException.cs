namespace Service.Application.Exceptions
{
    public class HttpException : AppException
    {
        public HttpException(string message, int statusCode) : base(message, statusCode)
        {
        }
    }
}
