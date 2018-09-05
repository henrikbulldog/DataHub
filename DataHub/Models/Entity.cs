using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class Entity
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; }
    }
}
