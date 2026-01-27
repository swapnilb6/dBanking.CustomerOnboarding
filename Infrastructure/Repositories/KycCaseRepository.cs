using Core.Entities;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Core.RepositoryContracts;
namespace Infrastructure.Repositories
{
    public sealed class KycCaseRepository : IKycCaseRepository
    {
        private readonly AppPostgresDbContext _db;
        public KycCaseRepository(AppPostgresDbContext db) => _db = db;

        public Task<KycCase?> GetByIdAsync(Guid caseId, CancellationToken ct) =>
            _db.KycCases.Include(x => x.Customer).FirstOrDefaultAsync(x => x.KycCaseId == caseId, ct);

        public Task<KycCase?> FindOpenForCustomerAsync(Guid customerId, CancellationToken ct) =>
            _db.KycCases.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.Status == KycStatus.PENDING, ct);

        public async Task<IReadOnlyList<KycCase>> GetByCustomerAsync(Guid customerId, CancellationToken ct) =>
            await _db.KycCases.Where(x => x.CustomerId == customerId)
                              .OrderByDescending(x => x.CreatedAt)
                              .ToListAsync(ct);

        public Task AddAsync(KycCase entity, CancellationToken ct) =>
            _db.KycCases.AddAsync(entity, ct).AsTask();

        public Task UpdateAsync(KycCase entity, CancellationToken ct)
        {
            _db.KycCases.Update(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}