using DataHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace DataHub.Repositories
{
    public class LocalDBContext : DbContext
    {
        public DbSet<SensorData> SensorData { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<Models.FileInfo> Files { get; set; }

        public LocalDBContext(ILogger logger = null)
        {
            Logger = logger;
            Database.EnsureDeleted();
            Database.EnsureCreated();
            SensorData.Add(new SensorData
            {
                Id = "1",
                Source = "Hist",
                TagId = "ABC123",
                Timestamp = DateTime.Now,
                Units = "Pa",
                Value = "123.2345",
                Tag = new Tag
                {
                    Id = "ABC123",
                    Name = "Some tag"
                }
            });
            SensorData.Add(new SensorData
            {
                Id = "2",
                Source = "Hist",
                TagId = "ABC234",
                Timestamp = DateTime.Now,
                Units = "Pa",
                Value = "23.23456",
                Tag = new Tag
                {
                    Id = "ABC234",
                    Name = "Some other tag"
                }
            });
            Files.Add(
                new Models.FileInfo
                {
                    Entity = "SensorData",
                    Format = "CSV",
                    Filename = "ABC123.CSV",
                    Id = "1",
                    Source = "Hist"
                });
            Files.Add(
                new Models.FileInfo
                {
                    Entity = "SensorData",
                    Format = "CSV",
                    Filename = "ABC124.CSV",
                    Id = "2",
                    Source = "Hist"
                });
            SaveChanges();
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
