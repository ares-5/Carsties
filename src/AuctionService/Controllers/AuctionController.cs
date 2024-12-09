using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionController(
    IAuctionRepository auctionRepository,
    IPublishEndpoint publishEndpoint,
    IMapper mapper)
    : ControllerBase
{
    private readonly IAuctionRepository auctionRepository = auctionRepository;
    private readonly IPublishEndpoint publishEndpoint = publishEndpoint;
    private readonly IMapper mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctionsAsync(string date)
    {
        return await auctionRepository.GetAuctionsAsync(date);
    }

    [HttpGet("{id}", Name = "GetAuctionById")]
    public async Task<ActionResult<AuctionDto>> GetAuctionByIdAsync(Guid id)
    {
        var auction = await auctionRepository.GetAuctionByIdAsync(id);
        if (auction is null)
        {
            return NotFound();
        }

        return auction;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuctionAsync(CreateAuctionDto createAuctionDto)
    {
        var auction = mapper.Map<Auction>(createAuctionDto);
        
        auction.Seller = User.Identity.Name;

        auctionRepository.AddAuction(auction);
        
        var newAuction = mapper.Map<AuctionDto>(auction);
        await publishEndpoint.Publish(mapper.Map<AuctionCreated>(newAuction));
        
        var result = await auctionRepository.SaveChangesAsync();

        if (!result)
        {
            return BadRequest("Couldn't save changes to the Db");
        }
        
        return CreatedAtRoute(
            "GetAuctionById",
            new { id = auction.Id },
            mapper.Map<AuctionDto>(auction)
        );
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuctionAsync(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await auctionRepository.GetAuctionEntityByIdAsync(id);
        if (auction is null)
        {
            return NotFound();
        }

        if (auction.Seller != User.Identity.Name)
        {
            return Forbid();
        }
        
        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
        
        await publishEndpoint.Publish(mapper.Map<AuctionUpdated>(auction));
        
        var result = await auctionRepository.SaveChangesAsync();
        if (result)
        {
            return Ok();
        }

        return BadRequest("Problem saving changes");
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuctionAsync(Guid id)
    {
        var auction = await auctionRepository.GetAuctionEntityByIdAsync(id);
        if (auction is null)
        {
            return NotFound();
        }

        if (auction.Seller != User.Identity.Name)
        {
            return Forbid();
        }
        
        auctionRepository.RemoveAuction(auction);
        
        await publishEndpoint.Publish(new AuctionDeleted { Id = auction.Id.ToString() });

        var result = await auctionRepository.SaveChangesAsync();

        if (!result)
        {
            return BadRequest("Problem saving changes");
        }

        return Ok();
    }
}