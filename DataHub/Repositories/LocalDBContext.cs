using DataHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DataHub.Repositories
{
    public class LocalDBContext : DbContext
    {
        public DbSet<TimeSeries> TimeSeries { get; set; }

        public DbSet<TimeSeriesTag> TimeSeriesTags { get; set; }

        public DbSet<Models.FileInfo> Files { get; set; }

        public DbSet<FunctionalAsset> FunctionalAssets { get; set; }

        public DbSet<SerialAsset> SerialAssets { get; set; }


        public LocalDBContext(ILogger logger = null)
        {
            Logger = logger;
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
        }

    }
}
