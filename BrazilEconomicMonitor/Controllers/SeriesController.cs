using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrazilEconomicMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeriesController: ControllerBase
    {
        private readonly BrazilEconomicMonitorDbContext _db;

        public SeriesController(BrazilEconomicMonitorDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<List<Series>> Get()
        {
            return await _db.Series.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<Series?> GetById(int id)
        {
            return await _db.Series
            .FirstOrDefaultAsync(s => s.Id == id);
        }

        [HttpPost]
        public async Task<Series> Create(Series series)
        {
            _db.Series.Add(series);

            await _db.SaveChangesAsync();

            return series;
        }
    }
}
