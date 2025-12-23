namespace MCHSProject.Common.Exceptions
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }

        public ApiException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : ApiException
    {
        public NotFoundException(string message) : base(message, 404) { }
    }

    public class UnauthorizedException : ApiException
    {
        public UnauthorizedException(string message = "Unauthorized") : base(message, 401) { }
    }

    public class ForbiddenException : ApiException
    {
        public ForbiddenException(string message = "Forbidden") : base(message, 403) { }
    }

    public class BadRequestException : ApiException
    {
        public BadRequestException(string message) : base(message, 400) { }
    }

    public class ConflictException : ApiException
    {
        public ConflictException(string message) : base(message, 409) { }
    }
}
