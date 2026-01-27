using Core.Entities;
using Core.RepositoryContracts;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public sealed class CustomerRepository : ICustomerRepository
    {
        //private readonly AppDBContext _db;
        private readonly AppPostgresDbContext _db;
        public CustomerRepository(AppPostgresDbContext db)
        {
            _db = db;
        }

        public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
        {
            // Include KYC cases if you frequently need them; keep it selective to avoid heavy loads.
            return await _db.Customers
                .Include(c => c.KycCases)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        }

        public async Task<bool> ExistsByEmailOrPhoneAsync(string email, string phone, CancellationToken ct = default)
        {
            return await _db.Customers
                .AnyAsync(c => c.Email == email || c.Phone == phone, ct);
        }

        public async Task<Customer?> GetByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default)
        {
            // Accepts either parameter; filters accordingly.
            return await _db.Customers
                .Where(c =>
                    (email != null && c.Email == email) ||
                    (phone != null && c.Phone == phone))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
        {
            await _db.Customers.AddAsync(customer, ct);
            return customer;
        }

        public Task UpdateAsync(Customer customer, CancellationToken ct = default)
        {
            // Entity should be tracked; setting state ensures updates when detached.
            _db.Entry(customer).State = EntityState.Modified;
            return Task.CompletedTask;
        }
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }

}
