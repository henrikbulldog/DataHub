using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class MessageRequest
    {
        public string Source { get; set; }
        public string Entity { get; set; }
        public string Time { get; set; }
        public string Payload { get; set; }
    }
}
