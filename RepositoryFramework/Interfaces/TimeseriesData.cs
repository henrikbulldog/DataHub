using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryFramework.Interfaces
{
    public class TimeseriesData
    {
        public string Tag { get; set; }

        public string Source { get; set; }

        public List<DataPoint> DataPoints { get; set; }
    }
}