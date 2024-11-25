using BiddingService.Models;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Services;

public class CheckAuctionFinished(
    IServiceProvider services,
    ILogger<CheckAuctionFinished> logger) : BackgroundService
{
    private readonly IServiceProvider services = services;
    private readonly ILogger<CheckAuctionFinished> logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting check for finished auctions");
        
        stoppingToken.Register(() => logger.LogInformation("==> Stopping check for finished auctions"));

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAuctionsAsync(stoppingToken);
            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task CheckAuctionsAsync(CancellationToken stoppingToken)
    {
        var finishedAuctions = await DB.Find<Auction>()
            .Match(x => x.AuctionEnd <= DateTime.UtcNow)
            .Match(x => !x.Finished)
            .ExecuteAsync(stoppingToken);

        if (finishedAuctions.Count == 0)
        {
            return;
        }
        
        logger.LogInformation("==> Found {count} finished auctions", finishedAuctions.Count);
        
        using var scope = services.CreateScope();
        var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        foreach (var finishedAuction in finishedAuctions)
        {
            finishedAuction.Finished = true;
            await finishedAuction.SaveAsync(null, stoppingToken);
            
            var winningBid = await DB.Find<Bid>()
                .Match(a => a.AuctionId.Equals(finishedAuction.ID))
                .Match(b => b.BidStatus == BidStatus.Accepted)
                .Sort(x => x.Descending(s => s.Amount))
                .ExecuteFirstAsync(stoppingToken);

            await endpoint.Publish(new AuctionFinished
            {
                AuctionId = finishedAuction.ID,
                ItemSold = winningBid != null,
                Winner = winningBid?.Bidder,
                Amount = winningBid?.Amount,
                Seller = finishedAuction.Seller
            }, stoppingToken);
        }
    }
}