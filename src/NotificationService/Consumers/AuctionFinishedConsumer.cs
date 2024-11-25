using Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

public class AuctionFinishedConsumer(IHubContext<NotificationHub> hubContext) : IConsumer<AuctionFinished>
{
    private readonly IHubContext<NotificationHub> hubContext = hubContext;

    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("==> Auction finished message received");
        
        await hubContext.Clients.All.SendAsync("AuctionFinished", context.Message);
    }
}