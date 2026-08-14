namespace BrazilEconomicMonitor.Domain.Entities
{
    public class Sources
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DocLink { get; set; } = string.Empty;
        public ICollection<Series> Series { get; set; } = new List<Series>();
    }
}
