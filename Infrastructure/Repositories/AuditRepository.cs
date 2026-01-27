using Core.Entities;
using Core.RepositoryContracts;
using Infrastructure.DbContext;

namespace Infrastructure.Repositories
{
    public sealed class AuditRepository : IAuditRepository
    {
        private readonly AppPostgresDbContext _db;

        public AuditRepository(AppPostgresDbContext db) => _db = db;

        public async Task AddAsync(AuditRecord audit, CancellationToken ct = default)
        {
            try
            {
                await _db.Set<AuditRecord>().AddAsync(audit, ct);
                await _db.SaveChangesAsync(ct); // Commit immediately; no updates allowed
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while adding the audit record.", ex);
            }
        }
    }
}
