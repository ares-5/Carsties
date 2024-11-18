using System.Globalization;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config)
{
    private readonly HttpClient httpClient = httpClient;
    private readonly IConfiguration config = config;

    public async Task<List<Item>> GetItemsForSearchDbAsync()
    {
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(i => i.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString())
            .ExecuteFirstAsync();

        return await httpClient.GetFromJsonAsync<List<Item>>(new Uri($"/api/auctions?date={lastUpdated}",
            UriKind.Relative));
    }
}