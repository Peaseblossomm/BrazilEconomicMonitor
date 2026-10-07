using System.Net;

namespace BrazilEconomicMonitor.CustomExceptions
{
    public class SeriesImportException: Exception
    {
        public string SeriesCode { get; }
        public string Source { get; }
        public HttpStatusCode? StatusCode { get; }
        public string? ReasonPhrase { get; }

        public SeriesImportException(
            string seriesCode,
            string source,
            Exception innerException)
            : base(
                  $"No Http response received. " +
                  $"Failed to import series {seriesCode} from {source}.",
                  innerException)
        {
            SeriesCode = seriesCode;
            Source = source;
        }

        public SeriesImportException(
            string seriesCode,
            string source,
            HttpStatusCode? statusCode,
            string? reasonPhrase
            )
            : base(
                  $"Http response was unsuccessful. +" +
                  $" StatusCode: {statusCode}; ReasonPhrase: {reasonPhrase}." +
                  $" Failed to import series {seriesCode} from {source}."
                  )
        {
            SeriesCode = seriesCode;
                Source = source;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
        }
    }
}
