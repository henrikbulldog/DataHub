using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public static class EntityExtensions
    {
        public static Entity FromType(this Entity entity, Type type)
        {
            entity.Name = type.Name;
            entity.Properties = new List<Property>();
            entity.Properties.AddRange(type.GetProperties()
                .Select(p => new Property
                {
                    Name = p.Name,
                    Datatype = p.PropertyType.Name
                }));
            return entity;
        }

        public static Type ToType(this Entity entity)
        {
            return Type.GetType($"DataHub.Entities.{entity.Name}, DataHub.Entities, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null");
        }

        private static object GetDefaultValue(string datatype)
        {
            switch (datatype.ToLowerInvariant())
            {
                case "datetime":
                    return default(DateTime);
                case "string":
                    return default(string);
                case "int":
                    return default(int);
                case "float":
                    return default(float);
                default:
                    return null;
            }
        }

        private static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}
