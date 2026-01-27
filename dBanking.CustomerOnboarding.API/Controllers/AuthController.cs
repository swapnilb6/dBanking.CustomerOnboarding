using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Text;

namespace dBanking.CustomerOnbaording.API.Controllers
{
    
    [ApiController]
    

    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // GET /auth/me -> basic identity & claims
        [HttpGet("api/Auth")]
        [Authorize] // token must be valid
        public IActionResult Me()
        {
            var user = HttpContext.User;
            var name = user.Identity?.Name ?? user.Claims.FirstOrDefault(c => c.Type == "cmsapi_user")?.Value;
            var scopes = user.Claims.Where(c => c.Type == "scp").Select(c => c.Value).ToArray();
            var appRoles = user.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray();

            return Ok(new
            {
                name,
                authenticated = user.Identity?.IsAuthenticated ?? false,
                scopes,
                appRoles,
                claims = user.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        // GET /auth/read-ping -> requires App.read
        [HttpGet("api/read-ping")]
        [Authorize(Policy = "App.read")]
        public IActionResult ReadPing() => Ok(new { message = "Read allowed" });

        // POST /auth/write-ping -> requires App.write
        [HttpPost("api/write-ping")]
        [Authorize(Policy = "App.write")]
        public IActionResult WritePing([FromBody] object payload) => Ok(new { message = "Write allowed", received = payload });
    }

}

