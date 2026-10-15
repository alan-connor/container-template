using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Container.Template.Api.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Hello_ReturnsGreeting()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hello World", json.RootElement.GetProperty("message").GetString());
        Assert.Equal("Container.Template.Api", json.RootElement.GetProperty("applicationName").GetString());
    }

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/healthz/live")]
    [InlineData("/healthz/ready")]
    public async Task HealthEndpoints_ReportHealthy(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_IsEnabledInDevelopment()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsDisabledInProduction()
    {
        var client = _factory
            .WithWebHostBuilder(b => b.UseEnvironment("Production"))
            .CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
