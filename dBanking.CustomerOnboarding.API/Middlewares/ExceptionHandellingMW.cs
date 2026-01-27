using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace dBanking.CustomerOnbaording.API.Middlewares
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExceptionHandellingMW
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandellingMW> _logger;
        public ExceptionHandellingMW(RequestDelegate next, ILogger<ExceptionHandellingMW> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.GetType().ToString()} : {ex.Message}");

                if (ex.InnerException is not null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.GetType().ToString()} : {ex.InnerException.Message}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await httpContext.Response.WriteAsJsonAsync(new { Message = ex.Message, Type = ex.GetType().ToString() });
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ExceptionHandellingMWExtensions
    {
        public static IApplicationBuilder UseExceptionHandellingMW(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandellingMW>();
        }
    }
}
