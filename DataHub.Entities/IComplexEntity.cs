using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    public interface IComplexEntity : IEntity
    {
        string ParentSource { get; set; }
        string ParentId { get; set; }
    }
}
