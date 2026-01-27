using Core.Entities;
using Core.Messages;
using Core.RepositoryContracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.Consumers
{
    /// <summary>
    /// Listens to KycStatusChanged events and updates Customer.Status accordingly.
    /// Idempotent: if the customer status already matches, does nothing.
    /// </summary>
    public sealed class KycStatusChangedConsumer : IConsumer<KycStatusChanged>
    {
        private readonly ICustomerRepository _customers;
        private readonly ILogger<KycStatusChangedConsumer> _logger;

        public KycStatusChangedConsumer(
            ICustomerRepository customers,
            ILogger<KycStatusChangedConsumer> logger)
        {
            _customers = customers;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<KycStatusChanged> context)
        {
            var msg = context.Message;
            var correlationId = msg.CorrelationId.ToString() ?? context.CorrelationId?.ToString() ?? string.Empty;

            _logger.LogInformation(
                "Consuming KycStatusChanged: CustomerId={CustomerId}, KycCaseId={KycCaseId}, Old={OldStatus}, New={NewStatus}, Corr={CorrelationId}",
                msg.CustomerId, msg.KycCaseId, msg.OldStatus, msg.NewStatus, correlationId);

            // Load customer
            var customer = await _customers.GetByIdAsync(msg.CustomerId, context.CancellationToken);
            if (customer is null)
            {
                _logger.LogWarning("Customer {CustomerId} not found for KycStatusChanged event.", msg.CustomerId);
                return; // No retry needed; consumer is idempotent and safe to skip
            }

            // Determine target CustomerStatus based on KYC
            var targetCustomerStatus = MapCustomerStatusFromKyc(msg.NewStatus);

            // Idempotency: only update if status actually changes
            if (customer.Status == targetCustomerStatus)
            {
                _logger.LogInformation(
                    "Customer {CustomerId} already in desired status {Status}; skipping update.",
                    msg.CustomerId, targetCustomerStatus);
                return;
            }

            // Update and persist
            var previous = customer.Status;
            customer.Status = targetCustomerStatus;
            customer.UpdatedAt = DateTime.UtcNow;

            await _customers.UpdateAsync(customer, context.CancellationToken);
            await _customers.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Customer {CustomerId} status updated {Old} -> {New} (KycCaseId={KycCaseId}, Corr={CorrelationId})",
                customer.CustomerId, previous, customer.Status, msg.KycCaseId, correlationId);
        }

        /// <summary>
        /// Business mapping rule: how KYC maps to Customer aggregate status.
        /// VERIFIED -> VERIFIED
        /// FAILED -> PENDING_KYC (retry or manual intervention)
        /// PENDING -> PENDING_KYC
        /// </summary>
        private static CustomerStatus MapCustomerStatusFromKyc(KycStatus kyc)
        {
            return kyc switch
            {
                KycStatus.VERIFIED => CustomerStatus.VERIFIED,
                KycStatus.FAILED => CustomerStatus.PENDING_KYC, // keep pending; domain may allow retries
                KycStatus.PENDING => CustomerStatus.PENDING_KYC,
                _ => CustomerStatus.PENDING_KYC
            };
        }
    }
}
