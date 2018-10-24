using DataHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections.Generic;

namespace DataHub.Repositories
{
    public class EntitiesDBContext : DbContext
    {
        public DbSet<EventInfo> Events { get; set; }

        public DbSet<FileInfo> Files { get; set; }

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

        public static readonly LoggerFactory ConsoleLoggerFactory
            = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

#if DEBUG
            if (Logger != null)
            {
                optionsBuilder.UseLoggerFactory(ConsoleLoggerFactory);
            }
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Asset>(asset =>
            {
                asset.HasKey(e => new { e.Source, e.Id });
                asset.Property(e => e.ParentId).IsRequired(false);
                asset.HasOne<Asset>()
                    .WithMany(e => e.Children as IEnumerable<Asset>)
                    .HasForeignKey(e => new { e.ParentSource, e.ParentId });
                asset.HasIndex(e => e.Tag);
            });

            modelBuilder.Entity<AssetTag>(tag =>
            {
                tag.HasKey(e => new { e.Source, e.Id });
                tag.HasOne<Asset>()
                .WithMany(e => e.Tags)
                .HasForeignKey(e => new { e.Source, e.AssetId });
            });

            modelBuilder.Entity<FileInfo>(file =>
            {
                file.HasKey(e => e.Id);
                file.Property(e => e.AssetId).IsRequired(false);
                file.HasOne<Asset>()
                .WithMany(e => e.Files)
                .HasForeignKey(e => new { e.Source, e.AssetId });
            });

            modelBuilder.Entity<TimeseriesMetadata>(ts =>
            {
                ts.HasKey(e => new { e.Source, e.Id });
                ts.Property(e => e.AssetId).IsRequired(false);
                ts.HasOne<Asset>()
                .WithMany(e => e.TimeSeries)
                .HasForeignKey(e => new { e.Source, e.AssetId });
                ts.Property(e => e.ParentId).IsRequired(false);
                ts.HasOne<TimeseriesMetadata>()
                    .WithMany(e => e.Children as IEnumerable<TimeseriesMetadata>)
                    .HasForeignKey(e => new { e.ParentSource, e.ParentId });
            });

            modelBuilder.Entity<EventInfo>(ts =>
            {
                ts.HasKey(e => e.Id);
            });
        }

    }
}
