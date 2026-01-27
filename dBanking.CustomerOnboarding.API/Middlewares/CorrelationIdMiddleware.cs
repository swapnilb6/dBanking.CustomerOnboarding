namespace dBanking.CustomerOnbaording.API.Middlewares
{
    public sealed class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var h) && !string.IsNullOrWhiteSpace(h)
                ? h.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("Correlation")
                   .BeginScope(new Dictionary<string, object> { [HeaderName] = correlationId }))
            {
                await _next(context);
            }
        }
    }
}
