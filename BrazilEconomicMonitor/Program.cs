using BrazilEconomicMonitor.BackgroundJobs;
using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services.InternalServices;
using BrazilEconomicMonitor.Services.QueryServices;
using BrazilEconomicMonitor.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BrazilEconomicMonitorDbContext>(
options =>
options.UseSqlite("Data Source=brazil.db"));

builder.Services.AddHttpClient<TreasuryApiClient>(client =>
{
    client.BaseAddress =
    new Uri("https://apiapex.tesouro.gov.br/aria/");
});

builder.Services.AddHttpClient<CentralBankApiClient>(client =>
{
    client.BaseAddress =
    new Uri("https://api.bcb.gov.br/dados/serie/");
});

builder.Services.AddHttpClient<CbOlindaApiClient>(client =>
{
    client.BaseAddress =
    new Uri("https://olinda.bcb.gov.br/olinda/servico/Expectativas/versao/v1/odata/");
});

builder.Services.Configure<ImportSettings>(builder.Configuration.GetSection("ImportSettings"));

// Add services to the container.   

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();


builder.Services.AddScoped<TreasuryImportService>();

builder.Services.AddScoped<CentralBankImportService>();

builder.Services.AddScoped<CbOlindaImportService>();

builder.Services.AddScoped<SeedDataCatalogService>();

builder.Services.AddScoped<HelperServices>();

builder.Services.AddScoped<TtmTransformationService>();

builder.Services.AddScoped<PrimaryBalanceOverGdpTransformationService>();

builder.Services.AddScoped<ForecastError12MonthsTransformationService>();

builder.Services.AddScoped<YoyTransformationService>();


builder.Services.AddScoped<DashboardQueryService>();


builder.Services.AddHostedService<FiscalDataImportWorker>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<BrazilEconomicMonitorDbContext>();

    bool hasSources =
        await db.Sources.AnyAsync();

    if (!hasSources)
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
            Name: "Derived Value",
            SourceDocLink: "",
            cancellationToken: CancellationToken.None);

        await service.SeedSeriesAsync(
            Name: "Primary Balance",
            Code: "10.07.1",
            SourceId: treasurySourceId,
            cancellationToken: CancellationToken.None);

        await service.SeedSeriesAsync(
            Name: "Nominal Balance",
            Code: "10.09.1",
            SourceId: treasurySourceId,
            cancellationToken: CancellationToken.None);

        await service.SeedSeriesAsync(
            Name: "Nominal GDP",
            Code: "4382",
            SourceId: centralBankSourceId,
            cancellationToken: CancellationToken.None);

        await service.SeedSeriesAsync(
            Name: "Nominal GDP",
            Code: "432",
            SourceId: centralBankSourceId,
            cancellationToken: CancellationToken.None);

        await service.SeedSeriesAsync(
            Name: "Nominal GDP",
            Code: "13522",
            SourceId: centralBankSourceId,
            cancellationToken: CancellationToken.None);

        await service.SeedSeriesAsync(
            Name: "Inflation Expectation 12 months",
            Code: "ExpectativasMercadoInflacao12Meses",
            SourceId: centralBankOlindaSourceId,
            cancellationToken: CancellationToken.None);
    }
} 

// Populate db with historical data if empty (first start) or with new additions latter
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

        var CentralBankImportService =
            scope.ServiceProvider
                .GetRequiredService<CentralBankImportService>();

        var ttmTransformationService =
            scope.ServiceProvider
                .GetRequiredService<TtmTransformationService>();

        var primaryBalanceOverGdpTransformationService =
            scope.ServiceProvider
                .GetRequiredService<PrimaryBalanceOverGdpTransformationService>();

        var forecastError12MonthsTransformationService =
            scope.ServiceProvider
                .GetRequiredService<ForecastError12MonthsTransformationService>();

        var yoyTransformationService =
            scope.ServiceProvider
                .GetRequiredService<YoyTransformationService>();

        await treasuryImportService.ImportFiscalAsync(
            "10.07.1",
            "01/2010",
            "",
            CancellationToken.None);

        await treasuryImportService.ImportFiscalAsync(
            "10.09.1",
            "01/2010",
            "",
            CancellationToken.None);

        await CentralBankImportService.ImportFiscalAsync(
            "4382",
            "01/01/2010",
            "",
            CancellationToken.None);

        await ttmTransformationService.SeedTtmAsync(CancellationToken.None);
        await primaryBalanceOverGdpTransformationService.SeedPrimaryBalanceOverGdpAsync(CancellationToken.None);
        await forecastError12MonthsTransformationService.SeedForecastError12MonthsASync(CancellationToken.None);
        await yoyTransformationService.SeedYoyAsync(CancellationToken.None);
    }
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
 