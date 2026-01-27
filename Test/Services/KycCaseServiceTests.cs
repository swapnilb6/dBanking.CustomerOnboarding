
using AutoMapper;
using Core.DTOS;
using Core.Entities;
using Core.Mappers;
using Core.Messages;
using Core.RepositoryContracts;
using Core.ServiceContracts;
using dBanking.Core.Services;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Services
{
    public sealed class KycCaseServiceTests : IAsyncLifetime
    {
        private readonly ServiceProvider _provider;
        private readonly ITestHarness _harness;

        private readonly Mock<IKycCaseRepository> _kycRepo = new(MockBehavior.Strict);
        private readonly Mock<ICustomerRepository> _custRepo = new(MockBehavior.Strict);

        private readonly IAuditService _audit;
        private readonly ICorrelationAccessor _corr;


        public KycCaseServiceTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton(_kycRepo.Object);
            services.AddSingleton(_custRepo.Object);

            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<KycMappingProfile>();
            });

            services.AddMassTransitTestHarness();

            _provider = services.BuildServiceProvider(true);
            _harness = _provider.GetRequiredService<ITestHarness>();
        }

        public async Task InitializeAsync() => await _harness.Start();
        public async Task DisposeAsync() => await _provider.DisposeAsync();

        [Fact]
        public async Task UpdateStatus_publishes_event_and_updates_customer_on_verified()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var custId = Guid.NewGuid();

            var kycCase = new KycCase
            {
                KycCaseId = caseId,
                CustomerId = custId,
                Status = KycStatus.PENDING
            };

            var customer = new Customer
            {
                CustomerId = custId,
                Status = CustomerStatus.PENDING_KYC
            };

            _kycRepo.Setup(r => r.GetByIdAsync(caseId, It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(kycCase);

            _kycRepo.Setup(r => r.UpdateAsync(kycCase, It.IsAny<System.Threading.CancellationToken>()))
                    .Returns(Task.CompletedTask);

            _kycRepo.Setup(r => r.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()))
                    .Returns(Task.CompletedTask);

            _custRepo.Setup(r => r.GetByIdAsync(custId, It.IsAny<System.Threading.CancellationToken>()))
                     .ReturnsAsync(customer);

            _custRepo.Setup(r => r.UpdateAsync(customer, It.IsAny<System.Threading.CancellationToken>()))
                     .Returns(Task.CompletedTask);

            _custRepo.Setup(r => r.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()))
                     .Returns((Task<int>)Task.CompletedTask);

            var mapper = _provider.GetRequiredService<IMapper>();
            var publishEndpoint = _provider.GetRequiredService<IPublishEndpoint>();

            IKycCaseService service = new KycCaseService(_kycRepo.Object, _custRepo.Object, mapper, publishEndpoint,_audit,_corr);

            var dto = new KycStatusUpdateRequestDto(
                CustomerId: custId,
                KycCaseId: caseId,
                Status: KycStatusDto.VERIFIED,
                ProviderRef: "prov-123",
                EvidenceRefs: new List<string> { "doc-1" },
                CheckedAt: DateTime.UtcNow
            );

            // Act
            var updated = await service.UpdateStatusAsync(dto, default);

            // Assert: entity updates
            updated.Status.Should().Be(KycStatus.VERIFIED);
            customer.Status.Should().Be(CustomerStatus.VERIFIED);

            // Assert: event published
            (await _harness.Published.Any<KycStatusChanged>()).Should().BeTrue();

            // Verify repo calls
            _kycRepo.VerifyAll();
            _custRepo.VerifyAll();
        }
    }
}
