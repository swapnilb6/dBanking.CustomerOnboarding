using Core.Entities;
using Core.Messages;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace dBanking.CustomerOnbaording.API.Controllers
{

    [ApiController]
    [Route("diag")]
    public class DiagController : ControllerBase
    {
        private readonly IPublishEndpoint _publish;
        public DiagController(IPublishEndpoint publish) => _publish = publish;

        [HttpPost("poke")]
        public async Task<IActionResult> Poke([FromServices] IPublishEndpoint _publish, CancellationToken ct)
        {

            var correlationId = Guid.NewGuid();

            await _publish.Publish<CustomerCreated>(new
            {
                CustomerId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                CreatedAtUtc = DateTime.UtcNow,
                SourceSystem = "CustomerApi",
                CorrelationId = correlationId
            }, ct);

            await _publish.Publish<KycStatusChanged>(new
            {
                KycCaseId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                NewStatus = KycStatus.VERIFIED,
                CheckedAt = DateTime.UtcNow,
                ProviderRef = "prov-001"
            }, ct);

            return Ok("published");

          
        }
    }
}