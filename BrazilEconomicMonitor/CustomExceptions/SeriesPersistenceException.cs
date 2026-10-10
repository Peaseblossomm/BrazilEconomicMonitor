namespace BrazilEconomicMonitor.CustomExceptions
{
    public class SeriesPersistenceException : Exception
    {
        public string SeriesCode { get; set; }
        public string DataSource { get; set; }



        public SeriesPersistenceException(string seriesCode, string source,
            Exception innerException)
            : base($"Failed to write series {seriesCode} from {source}.",
                      innerException)
        {
            SeriesCode = seriesCode;
            DataSource = source;
        }
    }
}
