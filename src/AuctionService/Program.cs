using AuctionService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(opts =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline

app.UseAuthorization();

app.MapControllers();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/auctions"), appBuilder =>
{
    appBuilder.Use(async (context, next) =>
    {
        var route = context.Request.Path;
        var method = context.Request.Method;
        Console.WriteLine($"Auction request hit: {method} {route}");
        await next.Invoke();
    });
});

try
{
    DbInitializer.InitDb(app);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

app.Run();