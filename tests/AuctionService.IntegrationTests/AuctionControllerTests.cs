using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

public class AuctionControllerTests(CustomWebAppFactory factory) : IClassFixture<CustomWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient httpClient = factory.CreateClient();

    [Fact]
    public async Task GetAuctions_ShouldReturn3Auctions()
    {
        // Act
        var response = await httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");
        
        // Assert
        Assert.Equal(3, response.Count);
    }
    
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        
        return Task.CompletedTask;
    }
}