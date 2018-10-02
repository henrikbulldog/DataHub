using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataHub.Models
{
    public class Entity
    {
        [Key]
        public string Name { get; set; }
        public List<Property> Properties { get; set; }
    }
}
