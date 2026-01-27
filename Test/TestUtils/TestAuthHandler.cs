using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Add claims your policies expect: "scp" with customer:read/write
        var claims = new[]
        {
            new Claim("sub", "test-user"),
            new Claim("scp", "customer:read customer:write"), // your policies split on space
            // If you use "scope" or the M365 URI claim, you can add those too
            new Claim("http://schemas.microsoft.com/identity/claims/scope", "customer:read customer:write"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
