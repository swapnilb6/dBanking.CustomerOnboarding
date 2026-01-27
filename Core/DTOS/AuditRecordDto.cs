using Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOS
{
    public sealed record AuditEntryDto(
        string EntityType,
        AuditAction Action,
        Guid? TargetEntityId,
        Guid? RelatedEntityId,
        string Actor,
        string? CorrelationId,
        object? BeforeSnapshot,
        object? AfterSnapshot,
        string? Source = null,
        string? Environment = null
    );
}
