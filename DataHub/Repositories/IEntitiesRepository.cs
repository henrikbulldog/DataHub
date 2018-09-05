using DataHub.Models;
using RepositoryFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Repositories
{
    public interface IEntitiesRepository : IGetById<Entity>, IGetByIdAsync<Entity>, IFind<Entity>, IFindAsync<Entity> 
    {
    }
}
