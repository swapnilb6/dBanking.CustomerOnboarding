using Core.Entities;

namespace Core.ServiceContracts
{
    /// <summary>
    /// Application service contract for customer-centric operations.
    /// Encapsulates business rules, validations, dedupe/idempotency, and event publishing.
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Creates a new customer (status = PENDING_KYC) after dedupe checks.
        /// Publishes a CustomerCreated event upon success.
        /// </summary>
        /// <param name="input">New customer aggregate (DTO mapped to domain).</param>
        /// <param name="idempotencyKey">Optional idempotency key to make retries safe.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<Customer> CreateAsync(Customer input, string? idempotencyKey = null, CancellationToken ct = default);

        /// <summary>
        /// Returns a customer by id (with KYC cases included when applicable).
        /// </summary>
        Task<Customer?> GetAsync(Guid customerId, CancellationToken ct = default);

        /// <summary>
        /// Lightweight search by email/phone.
        /// </summary>
        Task<Customer?> GetByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default);

        /// <summary>
        /// Updates a customer (basic fields). Service enforces business rules and audit logging.
        /// </summary>
        Task<Customer> UpdateAsync(Customer customer, CancellationToken ct = default);
    }

}
