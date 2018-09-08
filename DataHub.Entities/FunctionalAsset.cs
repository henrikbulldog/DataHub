using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    public class FunctionalAsset : Asset
    {
        public string TagNumber { get; set; }
        public string Location { get; set; }
        public IEnumerable<TimeSeriesTag> TimeSeriesTags { get; set; }
    }
}
