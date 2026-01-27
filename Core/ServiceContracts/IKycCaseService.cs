using Core.DTOS;
using Core.Entities;

namespace Core.ServiceContracts
{
    public interface IKycCaseService
    {
        Task<KycCase> StartForCustomerAsync(KycCaseCreateRequestDto dto, CancellationToken ct);
        Task<KycCase> UpdateStatusAsync(KycStatusUpdateRequestDto dto, CancellationToken ct);
        Task<KycCase?> GetByIdAsync(Guid caseId, CancellationToken ct);
        Task<IReadOnlyList<KycCase>> GetByCustomerAsync(Guid customerId, CancellationToken ct);
    }
}
