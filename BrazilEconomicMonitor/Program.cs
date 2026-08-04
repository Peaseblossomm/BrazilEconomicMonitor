using BrazilEconomicMonitor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BrazilEconomicMonitorDbContext>(
options =>
options.UseSqlite("Data Source=brazil.db"));

builder.Services.AddHttpClient<TreasuryApiClient>();
// Add services to the container.   

builder.Services.AddControllers();
builder.Services.AddOpenApi();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

/* app.MapGet("/seed", async (BrazilEconomicMonitorDbContext db) =>
{
    var series = new Series
    {
        Code = "Test",
        Name = "Series",
        Source = "Manual"
    };
    db.Series.Add(series);
    await db.SaveChangesAsync();
    return Results.Ok("Inserted");
});

app.MapGet("/series", async (BrazilEconomicMonitorDbContext db) =>
{
return await db.Series.ToListAsync();
}); */

app.Run();
 