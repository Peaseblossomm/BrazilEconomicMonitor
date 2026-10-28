namespace BrazilEconomicMonitor.Domain.Entities
{
    public class SelicRateIntervals
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Designation { get; set; } = "";


    }
}
