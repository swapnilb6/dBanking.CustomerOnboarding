using AutoMapper;
using Core.DTOS;
using Core.Entities;
using System.Text.Json;

namespace Core.Mappers
{
    public sealed class KycMappingProfile : Profile
    {
        public KycMappingProfile()
        {
            // DTO -> Entity
            CreateMap<KycCaseCreateRequestDto, KycCase>()
                // Persist JSONB
                .ForMember(d => d.EvidenceRefsJson, o => o.MapFrom((s, d) =>
                    s.EvidenceRefs == null ? "[]" : JsonSerializer.Serialize(s.EvidenceRefs)))

                // Ignore computed property
                .ForMember(d => d.EvidenceRefs, o => o.Ignore())

                .ForMember(dest => dest.KycCaseId, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
                .ForMember(d => d.Status, op => op.MapFrom(_ => KycStatus.PENDING))
                .ForMember(dest => dest.ConsentText, opt => opt.MapFrom(src => src.ConsentText))
                .ForMember(dest => dest.AcceptedAt, opt => opt.MapFrom(src => src.AcceptedAt))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.CheckedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProviderRef, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore());

            CreateMap<KycCase, KycCaseSummaryDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapStatus(src.Status)));
        }

        private static KycStatusDto MapStatus(KycStatus s) => s switch
        {
            KycStatus.PENDING => KycStatusDto.PENDING,
            KycStatus.VERIFIED => KycStatusDto.VERIFIED,
            KycStatus.FAILED => KycStatusDto.FAILED,
            _ => KycStatusDto.PENDING
        };
    }
}
