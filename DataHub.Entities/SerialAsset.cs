using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    public class SerialAsset : Asset
    {
        public string SerialNumber { get; set; }
        public string Producer { get; set; }
    }
}
