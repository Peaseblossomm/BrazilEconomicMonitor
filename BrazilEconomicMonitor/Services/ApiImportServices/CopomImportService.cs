using BrazilEconomicMonitor.CustomExceptions;
using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services;
using BrazilEconomicMonitor.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BrazilEconomicMonitor.Services.ApiImportServices
{
    public class CopomImportService
    {
        private readonly CopomApiClient _client;
        private readonly BrazilEconomicMonitorDbContext _db;
        private readonly ILogger<CopomImportService> _logger;

        CopomImportService(CopomApiClient client,
            BrazilEconomicMonitorDbContext db,
            ILogger<CopomImportService> logger
            )
        {
            _client = client;
            _db = db;
            _logger = logger;
        }

        public async Task ImportCopomLatestMeetingAsync(string count, CancellationToken cancellationToken)
        {
            string json = await _client.GetLatestCopomMeetingAsync(count, cancellationToken);

            List<CopomMeetingsRecordDto>? response;

            string seriesCode = "CopomComunicados";

            string source = "Central Bank Copom";

            try
            {
                response = JsonSerializer.Deserialize<List< CopomMeetingsRecordDto >> (
                    json,
                    new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = false });
            }
            catch (JsonException ex)
            {
                throw new SeriesImportException(
                    "CopomComunicados",
                    "Central Bank Copom",
                    ex
                    );
            }

            if (response == null)
            {
                throw new InvalidOperationException($"Api response for series {seriesCode} from source {source} is not found.");
            }

            Series? series = await _db.Series.Where(s => s.Code == seriesCode).SingleOrDefaultAsync(cancellationToken);

            if (series == null)
                throw new Exception($"No series were found referencing {source} the source and the given series code");

            foreach (CopomMeetingsRecordDto dto in response)
            {
                Observation? existing = await _db.Observations.Where(o => o.ObservationDate == dto.dataReferencia && o.SeriesId == series.Id)
                    .SingleOrDefaultAsync();

                if (existing == null)
                {
                    if (existing.Value != dto.nro_reuniao)
                    {
                        existing.Value = dto.nro_reuniao;
                        continue;
                    }
                continue;
                }

                Observation newMeeting = new Observation
                {
                    SeriesId = series.Id,
                    ObservationDate = dto.dataReferencia,
                    Value = dto.nro_reuniao
                };

                _db.Observations.Add(newMeeting);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task SeedSelicRateIntervalsAsync(CancellationToken cancellationToken)
        {

            DateTime seedStartingDate = new DateTime(2018, 1, 1);

            List<Observation> copomMeetings = await _db.Observations.Where(o => o.Series.Code == "CopomComunicados" && o.ObservationDate > seedStartingDate)
            .OrderBy(o => o.ObservationDate).ToListAsync(cancellationToken);                          // fati mu go redot tuka, ako 2018 1 1 e R12018, pocni od tamu da brois i da pravis novi Ri/YYYY

            if (copomMeetings.Count == 0)
            {
                throw new InvalidOperationException("No copom meetings observations are found");
            }

            int RInterval = 0;

            int currentYear = copomMeetings[0].ObservationDate.Year;

            for (int i = 0; i < copomMeetings.Count; i++)
            {
                int meetingYear = copomMeetings[i].ObservationDate.Year;

                DateTime startOfEffectiveRatePeriod = copomMeetings[i].ObservationDate.AddDays(1);   //StartDate

                DateTime? endOfEffectiveRatePeriod = null;                                           //EndDate

                if (i < copomMeetings.Count - 1)
                {
                    endOfEffectiveRatePeriod = copomMeetings[i + 1].ObservationDate;
                }

                RInterval++;                  //Interval number

                if (meetingYear != currentYear)                                    //R8/2025 to R1/2026    8 Brazil central bank meetings annually
                {
                    RInterval = 1;
                }

                string designation = $"R{RInterval.ToString()}/" +
                    $"{meetingYear.ToString()}";

                currentYear = meetingYear;

                SelicRateIntervals interval = new SelicRateIntervals
                {
                    StartDate = startOfEffectiveRatePeriod,
                    EndDate = endOfEffectiveRatePeriod,
                    Designation = designation
                };

                _db.SelicRateIntervals.Add(interval);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateSelicRateIntervalsAsync(CancellationToken cancellationToken)
        {
            Observation? latestCopomMeeting = await _db.Observations.Where(o => o.Series.Code == "CopomComunicados").OrderByDescending(o => o.ObservationDate).SingleOrDefaultAsync(cancellationToken);

            if (latestCopomMeeting == null)
            {
                throw new InvalidOperationException("Please use the ImportCopomLatestMeetingAsync to seed Copom Meetings data in the db");
            }

            SelicRateIntervals precedingInterval = await _db.SelicRateIntervals.OrderByDescending(o => o.StartDate).SingleOrDefaultAsync(cancellationToken);

            if (precedingInterval == null)
            {
                throw new InvalidOperationException("Please use the SeedSelicRateIntervalsAsync to seed  Rate Interval data in the db");
            }

            DateTime latestCopomMeetingDate = latestCopomMeeting.ObservationDate;

            if (latestCopomMeetingDate == precedingInterval.StartDate)  // No new data in the CopomMeetings series
            {
                return;
            }

            precedingInterval.EndDate = latestCopomMeetingDate;      // complete the preceding interval.  

            string latestIntervalDesignation = precedingInterval.Designation;


            int slashIndex = latestIntervalDesignation.IndexOf('/');
            string periodSubstring = latestIntervalDesignation.Substring(1, slashIndex - 1);
            int RInterval = int.Parse(periodSubstring);

            RInterval++;

            if (((DateTime)precedingInterval.EndDate).Year != precedingInterval.StartDate.Year)  //R8/2025 to R1/2026    8 Brazil central bank meetings annually
            {
                RInterval = 1;
            }



            string designation = $"R{RInterval.ToString()}/" + 
                                 $"{latestCopomMeetingDate.Year.ToString()}";
            
            DateTime startOfNewInterval = latestCopomMeetingDate.AddDays(1);  // Add the start date of the new interval

            SelicRateIntervals interval = new SelicRateIntervals
            {
                StartDate = startOfNewInterval,
                EndDate = null,
                Designation = designation
            };

            _db.SelicRateIntervals.Add(interval);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
