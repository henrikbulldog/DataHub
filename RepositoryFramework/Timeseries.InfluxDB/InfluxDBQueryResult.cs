namespace RepositoryFramework.Timeseries.InfluxDB
{
    internal class InfluxDBQueryResult
    {
        public string Error { get; set; }
        public InfluxDBResult[] Results { get; set; }
    }
}
