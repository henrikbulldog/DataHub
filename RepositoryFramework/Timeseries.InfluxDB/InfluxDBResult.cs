using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryFramework.Timeseries.InfluxDB
{
    internal class InfluxDBResult
    {
        public string Error { get; set; }
        public InfluxDBSerie[] Series { get; set; }
    }
}
