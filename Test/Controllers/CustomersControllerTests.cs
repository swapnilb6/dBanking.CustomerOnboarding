using System.Net;
using System.Net.Http.Json;
using Core.DTOS;
using Tests.TestUtils;
using FluentAssertions;
using Xunit;

namespace dBanking.Tests.Controllers
{
    public sealed class CustomersControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CustomersControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCustomerById_returns_200_with_payload_when_exists()
        {
            // Arrange: use a seeded ID from your DbContext.Seed() or insert a test customer here.

            var customerId = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"); // sample from your seed

            // Act
            var resp = await _client.GetAsync($"/api/customers/{customerId}");

            // Assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<CustomerResponseDto>();
            dto.Should().NotBeNull();
            dto!.CustomerId.Should().Be(customerId);
        }

        [Fact]
        public async Task Search_Returns_BadRequest_When_NoQueryProvided()
        {
            // Act
            var resp = await _client.GetAsync("/api/customers/search");

            // Assert
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            var body = await resp.Content.ReadAsStringAsync();
            body.Should().Contain("Provide at least 'email' or 'phone'");
        }

        [Fact]
        public async Task Search_Returns_NotFound_When_NoMatch()
        {
            // Arrange: use a randomized email unlikely to exist
            var randomEmail = $"no-such-user-{Guid.NewGuid():N}@example.test";

            // Act
            var resp = await _client.GetAsync($"/api/customers/search?email={System.Uri.EscapeDataString(randomEmail)}");

            // Assert
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Search_Returns_200_With_Payload_When_Found_By_Email()
        {
            // Arrange: create a new customer first
            var unique = Guid.NewGuid().ToString("N");
            var email = $"test.search.{unique}@example.test";

            var createDto = new CustomerCreateRequestDto(
                "Integration",
                "Test",
                System.DateOnly.FromDateTime(new System.DateTime(1990, 1, 1)),
                email,
                $"+100000{new System.Random().Next(1000, 9999)}",
                null
            );

            // Act: create customer
            var postResp = await _client.PostAsJsonAsync("/api/customers", createDto);

            // Assert: creation succeeded
            postResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            var created = await postResp.Content.ReadFromJsonAsync<CustomerResponseDto>();
            created.Should().NotBeNull();
            created!.Email.Should().Be(email);

            // Act: search by email
            var getResp = await _client.GetAsync($"/api/customers/search?email={System.Uri.EscapeDataString(email)}");

            // Assert: search returns the created customer
            getResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var found = await getResp.Content.ReadFromJsonAsync<CustomerResponseDto>();
            found.Should().NotBeNull();
            found!.CustomerId.Should().Be(created.CustomerId);
        }
    }
}
