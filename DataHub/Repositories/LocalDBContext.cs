using DataHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using DataHub.Models;

namespace DataHub.Repositories
{
    public class LocalDBContext : DbContext
    {
        public DbSet<Models.FileInfo> Files { get; set; }

        private IEntitiesRepository entitiesRepository;

        public LocalDBContext(
                IEntitiesRepository entitiesRepository,
                ILogger logger = null)
        {
            Logger = logger;
            this.entitiesRepository = entitiesRepository;
        }

        public ILogger Logger { get; set; }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            catch { }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
            }

            //if (Logger != null)
            //{
            //  var lf = new LoggerFactory();
            //  lf.AddProvider(new TestLoggerProvider(Logger));
            //  optionsBuilder.UseLoggerFactory(lf);
            //}
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entity in entitiesRepository.Find())
            {
                modelBuilder.Entity(entity.ToType());
            }
        }

    }
}
