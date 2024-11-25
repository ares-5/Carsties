using System.Globalization;
using AuctionService.Data;
using Grpc.Core;

namespace AuctionService.Services;

public class GrpcAuctionService(AuctionDbContext dbContext) : GrpcAuction.GrpcAuctionBase
{
    private readonly AuctionDbContext dbContext = dbContext;

    public override async Task<GrpcAuctionResponse> GetAuction(GetAuctionRequest request, ServerCallContext context)
    {
        Console.WriteLine("==> Received Grpc request for auction");
        
        var auction = await dbContext.Auctions.FindAsync(Guid.Parse(request.Id));

        if (auction is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Not found"));
        }
        
        var response = new GrpcAuctionResponse
        {
            Auction = new GrpcAuctionModel
            {
                AuctionEnd = auction.AuctionEnd.ToString(CultureInfo.InvariantCulture),
                Id = auction.Id.ToString(),
                ReservePrice = auction.ReservePrice,
                Seller = auction.Seller
            }
        };

        return response;
    }
}