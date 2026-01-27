using Core.DTOS;

namespace Core.ServiceContracts
{
    public interface IAuditService
    {
        Task RecordAsync(AuditEntryDto entry, CancellationToken ct = default);
    }

}
