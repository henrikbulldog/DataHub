using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
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
                c.SwaggerDoc("v1", new Info { Title = "Data Hub", Version = "v1" });
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "DataHub.xml");
                c.IncludeXmlComments(filePath);
                c.OperationFilter<FileOperationFilter>();
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
            if (dbContext.Set<TimeSeries>().Count() == 0)
            {
                var tag1 = new TimeSeriesTag
                {
                    OEMTagName = "ABC123",
                    Name = "Some tag",
                    Units = "Pa"
                };
                var tag2 = new TimeSeriesTag
                {
                    OEMTagName = "ABC234",
                    Name = "Some other tag",
                    Units = "Pa"
                };
                dbContext.Set<TimeSeriesTag>().Add(tag1);
                dbContext.Set<TimeSeriesTag>().Add(tag2);

                var r = new Random();
                for (int i = 1; i <= 1000; i++)
                {
                    var ts1 = dbContext.Set<TimeSeries>().Add(new TimeSeries
                    {
                        Source = "Historian",
                        TimeSeriesTagId = i % 2 == 0 ? 1 : 2,
                        Timestamp = DateTime.Now,
                        Value = r.NextDouble().ToString(CultureInfo.InvariantCulture),
                        Tag = i % 2 == 0 ? tag1 : tag2
                    });
                }

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
                                    TimeSeriesTags = new List<TimeSeriesTag> { tag1 },
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
                                    TimeSeriesTags = new List<TimeSeriesTag> { tag2 },
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
            }

            dbContext.SaveChanges();
        }
    }
}
