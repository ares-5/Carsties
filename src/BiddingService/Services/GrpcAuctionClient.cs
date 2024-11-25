using AuctionService;
using BiddingService.Models;
using Grpc.Net.Client;

namespace BiddingService.Services;

public class GrpcAuctionClient(
    IConfiguration config,
    ILogger<GrpcAuctionClient> logger)
{
    private readonly IConfiguration config = config;
    private readonly ILogger<GrpcAuctionClient> logger = logger;

    public Auction GetAuction(string id)
    {
        logger.LogInformation("Caling GRPC Service");

        var channel = GrpcChannel.ForAddress(config["GrpcAuction"]!);
        var client = new GrpcAuction.GrpcAuctionClient(channel);
        var request = new GetAuctionRequest { Id = id };

        try
        {
            var reply = client.GetAuction(request);
            var auction = new Auction
            {
                ID = reply.Auction.Id,
                AuctionEnd = DateTime.Parse(reply.Auction.AuctionEnd),
                Seller = reply.Auction.Seller,
                ReservePrice = reply.Auction.ReservePrice
            };

            return auction;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not call GRPC server");
            return null;
        }
    }
}