using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class MessageInfo : MessageRequest
    {
        public string  Id { get; set; }
        public string Uri { get; set; }
    }
}
