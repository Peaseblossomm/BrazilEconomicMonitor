using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services;
using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BrazilEconomicMonitorDbContext>(
options =>
options.UseSqlite("Data Source=brazil.db"));

builder.Services.AddHttpClient<TreasuryApiClient>(client =>
{
    client.BaseAddress =
    new Uri("https://apiapex.tesouro.gov.br/aria/");
});

// Add services to the container.   

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();

builder.Services.AddScoped<TreasuryImportService>();

builder.Services.AddScoped<DataSeriesCatalogService>();

builder.Services.AddHostedService<FiscalDataImportWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var service =
        scope.ServiceProvider
            .GetRequiredService<DataSeriesCatalogService>();

    await service.ImportDerivedSeriesManually(
        Name: "Primary Balance TTM",
        Code: "10.07.1_TTM",
        cancellationToken: CancellationToken.None);

    await service.ImportDerivedSeriesManually(
        Name: "Primary Balance YoY",
        Code: "10.07.1_YoY",
        cancellationToken: CancellationToken.None);

    await service.ImportDerivedSeriesManually(
        Name: "Nominal Balance TTM",
        Code: "10.09.1_TTM",
        cancellationToken: CancellationToken.None);

    await service.ImportDerivedSeriesManually(
        Name: "Nominal Balance YoY",
        Code: "10.09.1_YoY",
        cancellationToken: CancellationToken.None);


}
// Populate db with historical data if empty (first start)
using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<BrazilEconomicMonitorDbContext>();

    bool hasObservations =
        await db.Observations.AnyAsync();

    if (!hasObservations)
    {
        var treasuryImportService =
            scope.ServiceProvider
                .GetRequiredService<TreasuryImportService>();

        await treasuryImportService.ImportFiscalAsync(
            "10.07.1",
            "01/2015",
            null,
            CancellationToken.None);

    }
}

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
 