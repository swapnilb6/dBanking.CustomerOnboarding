using Core.Entities;
using Core.Messages;
using Core.RepositoryContracts;
using API.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Consumers
{
    public sealed class KycStatusChangedConsumerTests : IAsyncLifetime
    {
        private readonly ServiceProvider _provider;
        private readonly ITestHarness _harness;
        private readonly Mock<ICustomerRepository> _customers;

        public KycStatusChangedConsumerTests()
        {
            var services = new ServiceCollection();

            // Mocks
            _customers = new Mock<ICustomerRepository>(MockBehavior.Strict);

            services.AddLogging();
            services.AddSingleton(_customers.Object);

            // MassTransit TestHarness setup with in-memory transport
            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<KycStatusChangedConsumer>();

                cfg.AddConfigureEndpointsCallback((name, endpointConfigurator) =>
                {
                    // optional per-endpoint settings
                });
            });

            _provider = services.BuildServiceProvider(true);
            _harness = _provider.GetRequiredService<ITestHarness>();
        }

        public async Task InitializeAsync() => await _harness.Start();
        public async Task DisposeAsync() => await _provider.DisposeAsync();

        [Fact]
        public async Task Updates_customer_status_to_verified_when_kyc_verified()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var kycCaseId = Guid.NewGuid();

            var existingCustomer = new Customer
            {
                CustomerId = customerId,
                Status = CustomerStatus.PENDING_KYC
            };

            _customers.Setup(r => r.GetByIdAsync(customerId, It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(existingCustomer);

            _customers.Setup(r => r.UpdateAsync(existingCustomer, It.IsAny<System.Threading.CancellationToken>()))
                      .Returns(Task.CompletedTask);

            _customers.Setup(r => r.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .Returns((Task<int>)Task.CompletedTask);

            var bus = _provider.GetRequiredService<IBus>();

            // Act
            await bus.Publish<KycStatusChanged>(new
            {
                KycCaseId = kycCaseId,
                CustomerId = customerId,
                OldStatus = KycStatus.PENDING,
                NewStatus = KycStatus.VERIFIED,
                CheckedAtUtc = DateTime.UtcNow,
                ProviderRef = "prov-123",
                CorrelationId = customerId
            });

            // Assert: message consumed by our consumer
            (await _harness.Consumed.Any<KycStatusChanged>()).Should().BeTrue();

            // Assert: repository update invoked
            _customers.Verify(r => r.UpdateAsync(
                It.Is<Customer>(c => c.Status == CustomerStatus.VERIFIED),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _customers.Verify(r => r.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Is_idempotent_when_status_already_matches()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var kycCaseId = Guid.NewGuid();

            var existingCustomer = new Customer
            {
                CustomerId = customerId,
                Status = CustomerStatus.VERIFIED // already verified
            };

            _customers.Setup(r => r.GetByIdAsync(customerId, It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(existingCustomer);

            // No update/save expected
            var bus = _provider.GetRequiredService<IBus>();

            // Act
            await bus.Publish<KycStatusChanged>(new
            {
                KycCaseId = kycCaseId,
                CustomerId = customerId,
                OldStatus = KycStatus.VERIFIED,
                NewStatus = KycStatus.VERIFIED,
                CheckedAtUtc = DateTime.UtcNow,
                ProviderRef = "prov-123",
                CorrelationId = customerId
            });

            // Assert
            (await _harness.Consumed.Any<KycStatusChanged>()).Should().BeTrue();
            _customers.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
            _customers.Verify(r => r.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Gracefully_handles_missing_customer()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            _customers.Setup(r => r.GetByIdAsync(customerId, It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync((Customer?)null);

            var bus = _provider.GetRequiredService<IBus>();

            // Act
            await bus.Publish<KycStatusChanged>(new
            {
                KycCaseId = Guid.NewGuid(),
                CustomerId = customerId,
                OldStatus = KycStatus.PENDING,
                NewStatus = KycStatus.VERIFIED,
                CheckedAtUtc = DateTime.UtcNow,
                ProviderRef = "prov-123",
                CorrelationId = customerId
            });

            // Assert: consumed, but no update/save called
            (await _harness.Consumed.Any<KycStatusChanged>()).Should().BeTrue();
            _customers.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
            _customers.Verify(r => r.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }
    }
}
