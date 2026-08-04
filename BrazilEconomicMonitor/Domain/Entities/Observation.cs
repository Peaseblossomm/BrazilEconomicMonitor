namespace BrazilEconomicMonitor.Domain.Entities
{
    public class Observation
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public Series Series { get; set; } = null!;
        public DateTime ObservationDate { get; set; }
        public decimal Value { get; set; }

    }
}
