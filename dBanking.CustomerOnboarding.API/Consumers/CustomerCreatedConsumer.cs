
using Core.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
namespace API.Consumers
{
    public class CustomerCreatedConsumer : IConsumer<CustomerCreated>
    {
        private readonly ILogger<CustomerCreatedConsumer> _logger;

        public CustomerCreatedConsumer(ILogger<CustomerCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CustomerCreated> context)
        {
            var msg = context.Message;

            _logger.LogInformation(
                "Received CustomerCreated event: {CustomerId} - {FullName}",
                msg.CustomerId, msg.FirstName + " " + msg.LastName
            );

            // TODO: Add your business logic here
            // Example: Create user profile, send welcome email, etc.

            await Task.CompletedTask;
        }
    }
}
