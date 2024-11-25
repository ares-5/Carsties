using AutoMapper;
using BiddingService.Dtos;
using BiddingService.Models;
using BiddingService.Services;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace BiddingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController(
    IPublishEndpoint publishEndpoint,
    GrpcAuctionClient grpcClient,
    IMapper mapper) : ControllerBase
{
    private readonly IPublishEndpoint publishEndpoint = publishEndpoint;
    private readonly GrpcAuctionClient grpcClient = grpcClient;
    private readonly IMapper mapper = mapper;

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BidDto>> PlaceBidAsync(string auctionId, int amount)
    {
        var auction = await DB.Find<Auction>().OneAsync(auctionId);

        if (auction is null)
        {
            auction = grpcClient.GetAuction(auctionId);

            if (auction is null)
            {
                return BadRequest("Cannot accept bids on this auction at this time");
            }
        }

        if (auction.Seller.Equals(User.Identity?.Name))
        {
            return BadRequest("You cannot bid on your own auction");
        }

        var bid = new Bid
        {
            Amount = amount,
            AuctionId = auctionId,
            Bidder = User.Identity?.Name,
        };

        if (auction.AuctionEnd < DateTime.UtcNow)
        {
            bid.BidStatus = BidStatus.Finished;
        }
        else
        {
            var highBid = await DB.Find<Bid>()
                .Match(a => a.AuctionId.Equals(auctionId))
                .Sort(b => b.Descending(x => x.Amount))
                .ExecuteFirstAsync();

            if (highBid is not null && amount > highBid.Amount || highBid is null)
            {
                bid.BidStatus = amount > auction.ReservePrice
                    ? BidStatus.Accepted
                    : BidStatus.AcceptedBelowReserve;
            }

            if (highBid is not null && bid.Amount <= highBid.Amount)
            {
                bid.BidStatus = BidStatus.TooLow;
            }
        }

        await DB.SaveAsync(bid);

        await publishEndpoint.Publish(mapper.Map<BidPlaced>(bid));

        return Ok(mapper.Map<BidDto>(bid));
    }

    [HttpGet]
    public async Task<ActionResult<List<BidDto>>> GetBidsForAuctionAsync(string auctionId)
    {
        var bids = await DB.Find<Bid>()
            .Match(a => a.AuctionId.Equals(auctionId))
            .Sort(b => b.Descending(x => x.BidTime))
            .ExecuteAsync();

        return Ok(bids.Select(bid => mapper.Map<BidDto>(bid)).ToList());
    }
}