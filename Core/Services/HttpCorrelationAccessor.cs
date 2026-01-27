using Microsoft.AspNetCore.Http;


public interface ICorrelationAccessor
{
    string? Get();
}


namespace dBanking.Core.Services
{
   
    public sealed class HttpCorrelationAccessor : ICorrelationAccessor
    {
        private readonly IHttpContextAccessor _http;

        public HttpCorrelationAccessor(IHttpContextAccessor http) => _http = http;

        public string? Get() =>
            _http.HttpContext?.Items.TryGetValue("X-Correlation-Id", out var v) == true ? v as string : null;
    }
}
