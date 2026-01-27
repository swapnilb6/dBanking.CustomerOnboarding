using Core.Entities;

namespace Core.RepositoryContracts
{
    /// <summary>
    /// Repository contract for Customer persistence operations.
    /// Keeps EF Core details hidden from services/controllers.
    /// </summary>
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken ct = default);

        Task<bool> ExistsByEmailOrPhoneAsync(string email, string phone, CancellationToken ct = default);

        Task<Customer> AddAsync(Customer customer, CancellationToken ct = default);

        /// <summary>
        /// Returns tracked entity updated by caller; saves changes with unit-of-work pattern.
        /// </summary>
        Task UpdateAsync(Customer customer, CancellationToken ct = default);

        /// <summary>
        /// Persists all pending changes (unit-of-work commit).
        /// Prefer calling this from service layer to control transaction boundaries.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Optional: lightweight search by email/phone.
        /// </summary>
        Task<Customer?> GetByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default);
    }
}
