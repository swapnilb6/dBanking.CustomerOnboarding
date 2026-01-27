using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Xunit;

namespace dBanking.Tests.TestUtils
{
    public class ApiAuthTests
    {
        private const string TenantId = "8c701982-00dd-4c76-865e-ca4e2b05799b";
        private const string TestClientId = "6ae06f4b-9442-47c7-831e-88145cb001db"; // dBanking.CMS Tests
        private const string ApiClientId = "a0725bd6-efd4-4517-9083-959ccff1f1c5";      // dBanking.CMS API
        private readonly string[] Scopes = new[] { $"api://{ApiClientId}/App.read" };

        [Fact]
        public async Task ReadPing_ShouldSucceed_WithUserToken()
        {
            var authority = $"https://login.microsoftonline.com/{TenantId}";
            var pca = PublicClientApplicationBuilder.Create(TestClientId)
                                                   .WithAuthority(authority)
                                                   .Build();

            // Acquire delegated token via Device Code
            var authResult = await pca.AcquireTokenWithDeviceCode(Scopes, dcr =>
            {
                Console.WriteLine(dcr.Message); // "Go to https://microsoft.com/devicelogin and enter code ABCD..."
                return Task.CompletedTask;
            }).ExecuteAsync();

            using var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

            var resp = await client.GetAsync("auth/read-ping");
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Read allowed", body);
        }
    }

}
