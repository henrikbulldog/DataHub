using DataHub.Models;
using Microsoft.EntityFrameworkCore;
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
        
        public EntitiesRepository(string nspace = "DataHub.Entities")
        {
            var types = Assembly.Load($"{nspace}, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null")
                    .GetTypes()
                    .Where(t => t.IsClass && t.Namespace == nspace && !t.IsAbstract)
                    .Select(t => new Entity().FromType(t));
            entities.AddRange(types);
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
