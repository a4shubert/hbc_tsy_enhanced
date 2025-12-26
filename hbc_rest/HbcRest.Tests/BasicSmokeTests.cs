using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class BasicSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicSmokeTests(WebApplicationFactory<Program> factory)
    {
        // Point API to a throwaway SQLite file per test run.
        var tempDb = Path.Combine(Path.GetTempPath(), $"hbc_rest_test_{Guid.NewGuid():N}.db");
        Environment.SetEnvironmentVariable("HBC_DB_PATH", tempDb);
        _factory = factory;

        // Ensure database is created for this test host.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HbcRest.Data.HbcContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Swagger_is_available()
    {
        using HttpClient client = _factory.CreateClient();
        var resp = await client.GetAsync("/swagger/index.html");
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Survey_endpoint_returns_ok()
    {
        using HttpClient client = _factory.CreateClient();
        var resp = await client.GetAsync("/nyc_open_data_311_customer_satisfaction_survey");
        resp.EnsureSuccessStatusCode();
    }
}
