using Core.Entities;


namespace Core.RepositoryContracts
{
    public interface IKycCaseRepository
    {

        Task<KycCase?> GetByIdAsync(Guid caseId, CancellationToken ct);
        Task<KycCase?> FindOpenForCustomerAsync(Guid customerId, CancellationToken ct); // PENDING only (since enum has no IN_PROGRESS)
        Task<IReadOnlyList<KycCase>> GetByCustomerAsync(Guid customerId, CancellationToken ct);
        Task AddAsync(KycCase entity, CancellationToken ct);
        Task UpdateAsync(KycCase entity, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);

    }
}
