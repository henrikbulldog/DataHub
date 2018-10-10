using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class EventSubscriptionInfo
    {
        /// <summary>
        /// Event subscription connection uri
        /// </summary>
        public string ConnectionUri { get; set; }

        /// <summary>
        /// Event subscription protocol
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Uri to client documentation
        /// </summary>
        public string ClientDocumentationUri { get; set; }
    }
}
