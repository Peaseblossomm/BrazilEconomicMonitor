namespace BrazilEconomicMonitor.Domain.Entities
{
    public class Series 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsRaw { get; set; } = true;
        public int SourceId { get; set; }
        public Sources Sources { get; set; } = null!;

        public ICollection<Observation> Observations { get; set; } = new List<Observation>();
    }
}
