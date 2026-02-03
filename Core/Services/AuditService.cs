using Core.DTOS;
using Core.Entities;
using Core.RepositoryContracts;
using Core.ServiceContracts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Core.Services
{
  
    public sealed class AuditService : IAuditService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IAuditRepository _repo;
        //private readonly IClock _clock; // optional abstraction for time

        public AuditService(IAuditRepository repo)
        {
            _repo = repo;
        }


        public async Task RecordAsync(AuditEntryDto entry, CancellationToken ct = default)
        {
            var beforeJson = entry.BeforeSnapshot is null 
                ? null
                : JsonSerializer.Serialize(entry.BeforeSnapshot, JsonOptions);

            var afterJson = entry.AfterSnapshot is null
                ? null
                : JsonSerializer.Serialize(entry.AfterSnapshot, JsonOptions);

            var audit = new AuditRecord
            {
                EntityType = entry.EntityType,
                Action = entry.Action,                 // <-- use input
                TargetEntityId = entry.TargetEntityId,
                RelatedEntityId = entry.RelatedEntityId,
                Actor = entry.Actor,
                CorrelationId = entry.CorrelationId,
                Timestamp = DateTimeOffset.UtcNow,        // <-- simple & correct
                BeforeJson = beforeJson,                   // string -> jsonb OK
                AfterJson = afterJson,
                Source = entry.Source,
                Environment = entry.Environment
            };
            try
            {
                await _repo.AddAsync(audit, ct);
            }
            catch (DbUpdateException dbex)
            {
                //_logger.LogError(dbex, "Audit insert failed: {Message}", dbex.InnerException?.Message ?? dbex.Message);
                throw; // or wrap with inner exception details included
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Audit insert failed (unexpected).");
                throw;
            }
        }
    }
}
