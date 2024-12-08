using AuctionService.Controllers;
using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AuctionService.UnitTests.Utils;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionControllerTests
{
    private readonly Mock<IAuctionRepository> auctionRepo;
    private readonly Mock<IPublishEndpoint> publishEndpoint;
    private readonly Fixture fixture;
    private readonly AuctionController auctionController;
    private readonly IMapper mapper;

    public AuctionControllerTests()
    {
        fixture = new Fixture();
        auctionRepo = new Mock<IAuctionRepository>();
        publishEndpoint = new Mock<IPublishEndpoint>();

        var mockMapper = new MapperConfiguration(mc => { mc.AddMaps(typeof(MappingProfiles).Assembly); }).CreateMapper()
            .ConfigurationProvider;

        mapper = new Mapper(mockMapper);

        auctionController = new AuctionController(auctionRepo.Object, publishEndpoint.Object, mapper)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Helpers.GetClaimsPrincipal() }
            }
        };
    }

    [Fact]
    public async Task GetAuctions_WithNoParams_Returns10Auctions()
    {
        // Arrange
        var auctions = fixture.CreateMany<AuctionDto>(10).ToList();
        auctionRepo.Setup(repo => repo.GetAuctionsAsync(It.IsAny<string>()))
            .ReturnsAsync(auctions);

        // Act
        var result = await auctionController.GetAllAuctionsAsync(null);

        // Assert
        Assert.Equal(10, result.Value!.Count);
        Assert.IsType<ActionResult<List<AuctionDto>>>(result);
    }

    [Fact]
    public async Task GetAuctionById_WithValidGuid_ReturnsAuction()
    {
        // Arrange
        var auction = fixture.Create<AuctionDto>();
        auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);

        // Act
        var result = await auctionController.GetAuctionByIdAsync(auction.Id);

        // Assert
        Assert.Equal(auction.Make, result.Value!.Make);
        Assert.IsType<ActionResult<AuctionDto>>(result);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ReturnsNotFound()
    {
        // Arrange
        auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        // Act
        var result = await auctionController.GetAuctionByIdAsync(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuction_WithValidCreateAuctionDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var auction = fixture.Create<CreateAuctionDto>();
        
        auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        auctionRepo.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);

        // Act
        var result = await auctionController.CreateAuctionAsync(auction);
        var createdResult = result.Result as CreatedAtActionResult;

        // Assert
        Assert.NotNull(createdResult);
        Assert.Equal("GetAuctionByIdAsync", createdResult.ActionName);
        Assert.IsType<AuctionDto>(createdResult.Value);
    }
    
    [Fact]
    public async Task CreateAuction_FailedSave_Returns400BadRequest()
    {
        // Arrange
        var auctionDto = fixture.Create<CreateAuctionDto>();
        
        auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        auctionRepo.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(false);
        
        // Act
        var result = await auctionController.CreateAuctionAsync(auctionDto);
        
        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
    {
        // Arrange
        var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "test";
        var updateDto = fixture.Create<UpdateAuctionDto>();
       
        auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        auctionRepo.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);
        
        // Act
        var result = await auctionController.UpdateAuctionAsync(auction.Id, updateDto);
        
        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
    {
        // Arrange
        var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "different-user";
        var updateDto = fixture.Create<UpdateAuctionDto>();
        
        auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        auctionRepo.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);
        
        // Act
        var result = await auctionController.UpdateAuctionAsync(auction.Id, updateDto);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
    {
        // Arrange
        var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
        var updateDto = fixture.Create<UpdateAuctionDto>();
        
        auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null!);
    
        // Act
        var result = await auctionController.UpdateAuctionAsync(auction.Id, updateDto);
        
        // Assert
        Assert.IsType<NotFoundResult>(result);

    }

    [Fact]
    public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
    {
        // Arrange
        var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "test";
       
        auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        auctionRepo.Setup(repo => repo.RemoveAuction(It.IsAny<Auction>()));
        auctionRepo.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);
        
        // Act
        var result = await auctionController.DeleteAuctionAsync(auction.Id);
        
        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
    {
        // Arrange
        var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Seller = "test";
       
        auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);
        
        // Act
        var result = await auctionController.DeleteAuctionAsync(auction.Id);
        
        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidUser_Returns403Response()
    {
        // Arrange
        var auction = fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "different-user";
       
        auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        
        // Act
        var result = await auctionController.DeleteAuctionAsync(auction.Id);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}