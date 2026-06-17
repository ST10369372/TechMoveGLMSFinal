using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost; // <--- NEEDED FOR ConfigureTestServices
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechMoveGLMS.Data;
using TechMoveGLMS.Models;

namespace Tests.Integration
{
    /// <summary>
    /// Integration tests that spin up the real API in-memory and call its endpoints.
    /// Uses WebApplicationFactory so no real SQL Server or network is needed.
    /// These run automatically in the GitHub Actions CI pipeline.
    /// </summary>
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private string? _jwtToken;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Override DB with an in-memory database so tests are isolated
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // FIX: Use ConfigureTestServices to completely overwrite registrations safely
                builder.ConfigureTestServices(services =>
                {
                    // 1. Remove the real SQL Server registration
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // 2. EF CORE 9 FIX: Remove the internal EF Core 9 option configuration provider
                    var configDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>));
                    if (configDescriptor != null) services.Remove(configDescriptor);

                    // 3. Add in-memory EF Core cleanly
                    services.AddDbContext<ApplicationDbContext>(opts =>
                    {
                        opts.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid());
                    });
                });
            });
        }

        // ── Helper: get authenticated HttpClient ──────────────────
        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var client = _factory.CreateClient();

            if (_jwtToken == null)
            {
                var loginPayload = JsonSerializer.Serialize(
                    new { username = "admin", password = "Admin@GLMS2026" });

                var loginResp = await client.PostAsync(
                    "/api/auth/login",
                    new StringContent(loginPayload, Encoding.UTF8, "application/json"));

                Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

                var body = await loginResp.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                _jwtToken = json.GetProperty("token").GetString();
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _jwtToken);
            return client;
        }

        // ════════════════════════════════════════════════════════
        // AUTH TESTS
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            var client = _factory.CreateClient();
            var payload = JsonSerializer.Serialize(
                new { username = "techMove", password = "TechMove@2026!" });

            var resp = await client.PostAsync(
                "/api/auth/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var body = await resp.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            var token = json.GetProperty("token").GetString();
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var client = _factory.CreateClient();
            var payload = JsonSerializer.Serialize(
                new { username = "admin", password = "wrongpassword" });

            var resp = await client.PostAsync(
                "/api/auth/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_NoToken_Returns401()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/clients");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        
    }
}