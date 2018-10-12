﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    /// <summary>
    /// Event
    /// </summary>
    public class EventInfo : EventRequest
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public string  Id { get; set; }
    }
}
