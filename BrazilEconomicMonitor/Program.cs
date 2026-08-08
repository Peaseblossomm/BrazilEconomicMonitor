using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BrazilEconomicMonitorDbContext>(
options =>
options.UseSqlite("Data Source=brazil.db"));

builder.Services.AddHttpClient<TreasuryApiClient>(client =>
{
    client.BaseAddress =
    new Uri("https://apiapex.tesouro.gov.br/aria/");
});

builder.Services.AddScoped<ImportService>();

// Add services to the container.   

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ImportService>();

builder.Services.AddScoped<TreasurySeriesCatalogService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var catalogService =
        scope.ServiceProvider
            .GetRequiredService<TreasurySeriesCatalogService>();

    await catalogService.ImportSelectedSeriesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI();
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
 