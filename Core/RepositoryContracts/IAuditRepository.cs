using Core.Entities;

namespace Core.RepositoryContracts
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditRecord audit, CancellationToken ct = default);
    }
}
