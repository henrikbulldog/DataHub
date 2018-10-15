﻿using DataHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataHub.Repositories
{
    public class EntitiesDBContext : DbContext
    {
        public DbSet<Entities.EventInfo> Events { get; set; }

        public DbSet<Entities.FileInfo> Files { get; set; }

        private IEntitiesRepository entitiesRepository;

        private string connectionString;

        public EntitiesDBContext(
            string connectionString,
            IEntitiesRepository entitiesRepository,
            ILogger logger = null)
        {
            this.connectionString = connectionString;
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
                optionsBuilder.UseSqlServer(connectionString);
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
