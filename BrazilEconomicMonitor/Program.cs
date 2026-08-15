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

builder.Services.AddScoped<SeedDataCatalogService>();

builder.Services.AddHostedService<FiscalDataImportWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var service =
        scope.ServiceProvider
            .GetRequiredService<SeedDataCatalogService>();

    int treasurySourceId = await service.SeedSourcesAsync(
        Name: "Treasury",
        SourceDocLink: "https://sisweb.tesouro.gov.br/apex/f?p=10250:7:101490171757515::NO:7:P7_ID_PROJETO:1766",
        cancellationToken: CancellationToken.None);

    int centralBankSourceId = await service.SeedSourcesAsync(
        Name: "Central Bank",
        SourceDocLink: "https://www3.bcb.gov.br/sgspub/localizarseries/localizarSeries.do?method=prepararTelaLocalizarSeries",
        cancellationToken: CancellationToken.None);

    int centralBankOlindaSourceId = await service.SeedSourcesAsync(
        Name: "Central Bank Olinda",
        SourceDocLink: "https://olinda.bcb.gov.br/olinda/service/Expectativas/version/v1/swagger-ui3",
        cancellationToken: CancellationToken.None);

    int DerivedValueId = await service.SeedSourcesAsync(
        Name: "Derived value",
        SourceDocLink: "",
        cancellationToken: CancellationToken.None);

    await service.SeedSeriesAsync(
        Name: "Primary Balance",
        Code: "10.07.1_",
        SourceId: treasurySourceId,
        cancellationToken: CancellationToken.None);

    await service.SeedSeriesAsync(
        Name: "Nominal Balance",
        Code: "10.09.1",
        SourceId: treasurySourceId,
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
            .GetRequiredService<DataSeriesCatalogService>();

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
 