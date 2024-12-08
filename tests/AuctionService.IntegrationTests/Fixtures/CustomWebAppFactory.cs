using AuctionService.Data;
using AuctionService.IntegrationTests.Utils;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AuctionService.IntegrationTests.Fixtures;

public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer postgreSqlContainer = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await postgreSqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveDbContext<AuctionDbContext>();
            
            services.AddDbContext<AuctionDbContext>(opts =>
            {
                opts.UseNpgsql(postgreSqlContainer.GetConnectionString());
            });

            services.AddMassTransitTestHarness();
            
            services.EnsureCreated<AuctionDbContext>();           
        });
    }

    Task IAsyncLifetime.DisposeAsync() => postgreSqlContainer.DisposeAsync().AsTask();
}