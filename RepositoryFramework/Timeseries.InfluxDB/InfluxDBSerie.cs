using System.Collections.Generic;

namespace RepositoryFramework.Timeseries.InfluxDB
{
    internal class InfluxDBSerie
    {
        public InfluxDBSerie()
        {
            Tags = new Dictionary<string, string>();
            Columns = new string[] { };
            Values = new object[][] { };
        }

        private InfluxDBSerie(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public string[] Columns { get; set; }
        public object[][] Values { get; set; }
    }
}
