namespace BrazilEconomicMonitor.Domain.Entities
{
    public class Series 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public ICollection<Observation> Observations { get; set; } = new List<Observation>();
    }
}
