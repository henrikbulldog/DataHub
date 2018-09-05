using DataHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DataHub.Repositories
{
    public class EntitiesRepository : IEntitiesRepository
    {
        private List<Entity> entities = new List<Entity>();

        public EntitiesRepository()
        {
            string nspace = "DataHub.Entities";
            var q = from t in Assembly.Load("DataHub.Entities, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null").GetTypes()
                    where t.IsClass && t.Namespace == nspace
                    select t;
            entities.AddRange(q.Select(t => new Entity().FromType(t)));
        }

        public IEnumerable<Entity> Find()
        {
            return entities;
        }

        public async Task<IEnumerable<Entity>> FindAsync()
        {
            return await Task.Run(() => Find());
        }

        public Entity GetById(object id)
        {
            return entities.FirstOrDefault(e => e.Name == id.ToString());
        }

        public async Task<Entity> GetByIdAsync(object id)
        {
            return await Task.Run(() => GetById(id));
        }
    }
}
