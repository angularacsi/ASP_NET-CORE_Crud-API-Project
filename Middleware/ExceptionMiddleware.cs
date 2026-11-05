using System.Net;
using Crud.Dtos;
using Crud.Exceptions;

namespace Crud.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext); // this part allows the request to continue to the next middleware in the pipeline
            }
            catch (Exception ex)
            {
                // If any problem happnes we have to log the error first
                _logger.LogError(ex, "Unhandled exception occured");

                var statusCode = ex switch
                {
                    UserAlreadyExistsException => StatusCodes.Status409Conflict,
                    InvalidCredentialsException => StatusCodes.Status401Unauthorized,
                    UserNotFoundException => StatusCodes.Status404NotFound,
                    InactiveAccountException => StatusCodes.Status403Forbidden,
                    InvalidRefreshTokenException => StatusCodes.Status401Unauthorized,
                    _ => StatusCodes.Status500InternalServerError,
                };

                httpContext.Response.StatusCode = statusCode;

                httpContext.Response.ContentType = "application/json";

                var message = ex switch
                {
                    UserAlreadyExistsException => "User already exists.",
                    InvalidCredentialsException => "Invalid credentials.",
                    UserNotFoundException => "User not found.",
                    InactiveAccountException => "Account is inactive.",
                    InvalidRefreshTokenException => "Invalid refresh token.",
                    _ => "An unexpected error occurred.",
                };

                var response = new ErrorResponseDto
                {
                    Success = false,
                    StatusCode = statusCode,
                    Message = message,
                };
                await httpContext.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
