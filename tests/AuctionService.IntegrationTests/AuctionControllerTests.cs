using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("Shared collection")]
public class AuctionControllerTests(CustomWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient httpClient = factory.CreateClient();
    private const string GtId = "afbee524-5972-4075-8800-7d1f9d7b0a0c";
    
    [Fact]
    public async Task GetAuctions_ShouldReturn3Auctions()
    {
        // Act
        var response = await httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");
        
        // Assert
        Assert.Equal(3, response.Count);
    }
    
    [Fact]
    public async Task GetAuctionById_WithValidId_ShouldReturnAuction()
    {
        // Act
        var response = await httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{GtId}");
        
        // Assert
        Assert.Equal("GT", response.Model);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ShouldReturn404()
    {
        // Act
        var response = await httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ShouldReturn400()
    {
        // Act
        var response = await httpClient.GetAsync("api/auctions/not-a-guid");
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateAuction_WithNoAuth_ShouldReturn401()
    {
        // Arrange 
        var auction = new CreateAuctionDto
        {
            Make = "test",
            Model = "test",
            Year = 0,
            Color = "test",
            Mileage = 0,
            ImageUrl = "test",
            ReservePrice = 0,
            AuctionEnd = default
        };
        
        // Act
        var response = await httpClient.PostAsJsonAsync("api/auctions", auction);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateAuction_WithAuth_ShouldReturn201()
    {
        // Arrange 
        var auction = GetAuctionForCreate();
        httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));
        
        // Act
        var response = await httpClient.PostAsJsonAsync("api/auctions", auction);
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal("bob", createdAuction.Seller);
    }
    
    [Fact]
    public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
    {
        // Arrange 
        var auction = GetAuctionForCreate();
        auction.Make = null;
        httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));
        
        // Act
        var response = await httpClient.PostAsJsonAsync("api/auctions", auction);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
    {
        // Arrange
        var updateAuction = new UpdateAuctionDto { Make = "Updated" };
        httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await httpClient.PutAsJsonAsync($"api/auctions/{GtId}", updateAuction);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
    {
        // Arrange
        var updateAuction = new UpdateAuctionDto { Make = "Updated" };
        httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("notbob"));

        // Act
        var response = await httpClient.PutAsJsonAsync($"api/auctions/{GtId}", updateAuction);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        
        return Task.CompletedTask;
    }

    private static CreateAuctionDto GetAuctionForCreate()
    {
        return new CreateAuctionDto
        {
            Make = "test",
            Model = "testModel",
            ImageUrl = "test",
            Color = "test",
            Mileage = 10,
            Year = 10,
            ReservePrice = 10,
            AuctionEnd = default
        };
    }
}