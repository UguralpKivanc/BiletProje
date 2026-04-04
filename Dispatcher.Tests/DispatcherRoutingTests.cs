using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dispatcher.Tests;

public class DispatcherRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DispatcherRoutingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetEvents_WithoutCredentials_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithoutCredentials_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UnknownServicePath_WithApiKey_Returns400()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "KingoSifre123");
        var res = await client.GetAsync("/api/invalid-service/xyz");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
