using System;
using System.Collections.Generic;
using DataHub.Entities;
using DataHub.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;
using Swashbuckle.AspNetCore.Swagger;

namespace DataHub
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped(
                typeof(IEntitiesRepository),
                sp => new EntitiesRepository());

            services.AddScoped(
                typeof(DbContext),
                sp =>
                {
                    var context = new LocalDBContext(sp.GetService<IEntitiesRepository>());
                    Seed(context);
                    return context;
                });

            services.AddScoped(
                typeof(IBlobRepository),
                sp => new BlobRepository());

            services.AddScoped(
                typeof(IQueryableRepository<Models.FileInfo>),
                sp => new EntityFrameworkRepository<Models.FileInfo>(sp.GetService<DbContext>()));

            services.AddScoped(
                typeof(IQueryableRepository<Models.EventInfo>),
                sp => new EntityFrameworkRepository<Models.EventInfo>(sp.GetService<DbContext>()));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
        private void Seed(LocalDBContext dbContext)
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            var ts1 = dbContext.Set<TimeSeries>().Add(new TimeSeries
            {
                Id = "1",
                Source = "Hist",
                TimeSeriesTagId = "1",
                Timestamp = DateTime.Now,
                Value = "123.2345",
                Tag = new TimeSeriesTag
                {
                    Id = "1",
                    OEMTagName = "ABC123",
                    Name = "Some tag",
                    Units = "Pa"
                }
            });
            var ts2 = dbContext.Set<TimeSeries>().Add(new TimeSeries
            {
                Id = "2",
                Source = "Hist",
                TimeSeriesTagId = "2",
                Timestamp = DateTime.Now,
                Value = "23.23456",
                Tag = new TimeSeriesTag
                {
                    Id = "2",
                    OEMTagName = "ABC234",
                    Name = "Some other tag",
                    Units = "Pa"
                }
            });

            dbContext.Set<Models.EventInfo>()
                .Add(new Models.EventInfo
                {
                    Id = "1",
                    Source = "Source",
                    Time = DateTime.Now,
                    Type = "Type"
                });
            dbContext.Set<Models.EventInfo>()
                .Add(new Models.EventInfo
                {
                    Id = "2",
                    Source = "Source",
                    Time = DateTime.Now,
                    Type = "Type"
                });

            dbContext.Set<Models.FileInfo>().Add(
                new Models.FileInfo
                {
                    Entity = "SensorData",
                    Format = "CSV",
                    Filename = "ABC123.CSV",
                    Id = "1",
                    Source = "Hist"
                });
            dbContext.Set<Models.FileInfo>().Add(
                new Models.FileInfo
                {
                    Entity = "SensorData",
                    Format = "CSV",
                    Filename = "ABC124.CSV",
                    Id = "2",
                    Source = "Hist"
                });
            dbContext.Set<ReferenceAsset>().Add(
                new ReferenceAsset
                {
                    Id = "1",
                    Name = "Top drive",
                    SFITag = "313-M01",
                    SubAssets = new List<ReferenceAsset>
                    {
                        new ReferenceAsset
                        {
                            Id = "2",
                            Name = "Drivers",
                            SFITag = "313-M01-01"
                        },
                        new ReferenceAsset
                        {
                            Id = "3",
                            Name = "Gear",
                            SFITag = "313-M01-02"
                        }
                    }
                });
            dbContext.Set<Site>().Add(
                new Site
                {
                    Id = "1",
                    Name = "Site 1",
                    FunctionalAssets = new List<FunctionalAsset>
                    {
                        new FunctionalAsset
                        {
                            Id = "1",
                            SiteId = "1",
                            ReferenceAssetId = "1",
                            Location = "Location 1",
                            Name = "Top drive 123",
                            TagNumber = "313-M01-01-123",
                            SubAssets = new List<FunctionalAsset>
                            {
                                new FunctionalAsset
                                {
                                    Id = "2",
                                    SiteId = "1",
                                    ReferenceAssetId = "2",
                                    Location = "Location 2",
                                    Name = "Top drive driver 123",
                                    TagNumber = "313-M01-01-123",
                                    TimeSeriesTags = new List<TimeSeriesTag> { ts1.Entity.Tag },
                                    SerialAssets = new List<SerialAsset>
                                    {
                                        new SerialAsset
                                        {
                                            Id = "1",
                                            Name = "Driver XZY 1234",
                                            Producer = "Some producer",
                                            SerialNumber = "1234568790"
                                        }
                                    }
                                },
                                 new FunctionalAsset
                                {
                                    Id = "3",
                                    SiteId = "1",
                                    ReferenceAssetId = "3",
                                    Location = "Location 3",
                                    Name = "Top drive gear 123",
                                    TagNumber = "313-M01-02-123",
                                    TimeSeriesTags = new List<TimeSeriesTag> { ts2.Entity.Tag },
                                    SerialAssets = new List<SerialAsset>
                                    {
                                        new SerialAsset
                                        {
                                            Id = "2",
                                            Name = "Gear XZY 1234",
                                            Producer = "Some producer",
                                            SerialNumber = "1234568790"
                                        }
                                    }
                                }
}
                        }
                    }
                });
            dbContext.SaveChanges();
        }
    }
}
