using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Contollers;

[ApiController]
[Route("api/auctions")]
public class AuctionController(
    AuctionDbContext context,
    IPublishEndpoint publishEndpoint,
    IMapper mapper)
    : ControllerBase
{
    private readonly AuctionDbContext context = context;
    private readonly IPublishEndpoint publishEndpoint = publishEndpoint;
    private readonly IMapper mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctionsAsync(string date)
    {
        var query = context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        if (!string.IsNullOrWhiteSpace(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }
        
        return await query.ProjectTo<AuctionDto>(mapper.ConfigurationProvider).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionByIdAsync(Guid id)
    {
        var auction = await context.Auctions
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction is null)
        {
            return NotFound();
        }

        return mapper.Map<AuctionDto>(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuctionAsync(CreateAuctionDto createAuctionDto)
    {
        var auction = mapper.Map<Auction>(createAuctionDto);
        auction.Seller = "test";

        context.Auctions.Add(auction);
        
        var newAuction = mapper.Map<AuctionDto>(auction);
        await publishEndpoint.Publish(mapper.Map<AuctionCreated>(newAuction));
        
        var result = await context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest("Couldn't save changes to the Db");
        }
        
        return CreatedAtAction(
            nameof(GetAuctionByIdAsync),
            new { id = auction.Id },
            mapper.Map<AuctionDto>(auction)
        );
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuctionAsync(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await context.Auctions.Include(x => x.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction is null)
        {
            return NotFound();
        }
        
        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
        
        await publishEndpoint.Publish(mapper.Map<AuctionUpdated>(auction));
        
        var result = await context.SaveChangesAsync() > 0;

        if (result)
        {
            return Ok();
        }

        return BadRequest("Problem saving changes");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuctionAsync(Guid id)
    {
        var auction = await context.Auctions.FindAsync(id);

        if (auction is null)
        {
            return NotFound();
        }
        
        context.Auctions.Remove(auction);
        
        await publishEndpoint.Publish(new AuctionDeleted { Id = auction.Id.ToString() });
        
        var result = await context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest("Problem saving changes");
        }

        return Ok();
    }
}