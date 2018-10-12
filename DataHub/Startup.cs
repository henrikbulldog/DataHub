using DataHub.Entities;
using DataHub.Hubs;
using DataHub.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;
using RepositoryFramework.Timeseries.InfluxDB;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DataHub
{
    public class Startup
    {
        public const string EVENT_HUB_PATH = "/event-hub";

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
                typeof(EntitiesDBContext),
                sp =>
                {
                    var context = new EntitiesDBContext(
#if RELEASE
                        Environment.GetEnvironmentVariable("Azure.Sql.Connection"),
#else
                        @"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0",
#endif
                    sp.GetService<IEntitiesRepository>());
                    context.Database.EnsureCreated();
                    Seed(context);
                    return context;
                });

            services.AddScoped(
                typeof(IQueryableRepository<Models.FileInfo>),
                sp => new EntityFrameworkRepository<Models.FileInfo>(sp.GetService<EntitiesDBContext>()));

            services.AddScoped(
                typeof(IQueryableRepository<Models.EventInfo>),
                sp => new EntityFrameworkRepository<Models.EventInfo>(sp.GetService<EntitiesDBContext>()));

#if RELEASE
            services.AddScoped(
                typeof(IBlobRepository),
                sp => new AzureBlobRepository(CloudStorageAccount
                    .Parse(Environment.GetEnvironmentVariable("Azure.Storage.Connection"))
                    .CreateCloudBlobClient()
                    .GetContainerReference(Configuration["Azure.Blob:Container"])));
#else
            services.AddScoped(
                typeof(IBlobRepository),
                sp => new BlobRepository());
#endif

            services.AddScoped(
                typeof(ITimeseriesRepository),
                sp => new InfluxDBRepository("http://localhost:8086", "datahub", "ComputerInfo"));

            services.AddScoped(
                typeof(IQueryableRepository<Models.TimeseriesMetadata>),
                sp => new EntityFrameworkRepository<Models.TimeseriesMetadata>(sp.GetService<EntitiesDBContext>()));

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
                c.CustomSchemaIds(type => type.FriendlyId().Replace("[", "Of").Replace("]", ""));
            });

            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .AllowAnyOrigin();
            }));

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<EntitiesDBContext>();
                context.Database.EnsureCreated();
                Seed(context);
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("CorsPolicy");

            app.UseSignalR(routes =>
            {
                routes.MapHub<EventHub>(EVENT_HUB_PATH);
            });

#if RELEASE
            app.ApplyUserKeyValidation();
#endif

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Data Hub");
            });
        }

        private void Seed(EntitiesDBContext dbContext)
        {
            if (dbContext.Set<Entities.DataPoint>().Count() == 0)
            {
                var tag1 = new TimeSeries
                {
                    Name = "ABC123",
                    Description = "Some tag",
                    Units = "Pa"
                };
                var tag2 = new TimeSeries
                {
                    Name = "ABC234",
                    Description = "Some other tag",
                    Units = "Pa"
                };
                dbContext.Set<TimeSeries>().Add(tag1);
                dbContext.Set<TimeSeries>().Add(tag2);

                var r = new Random();
                for (int i = 1; i <= 1000; i++)
                {
                    var ts1 = dbContext.Set<Entities.DataPoint>().Add(new Entities.DataPoint
                    {
                        Source = "Historian",
                        TimeSeriesId = i % 2 == 0 ? 1 : 2,
                        Timestamp = DateTime.Now,
                        Value = r.NextDouble().ToString(CultureInfo.InvariantCulture)
                    });
                }

                dbContext.Set<Models.EventInfo>()
                    .Add(new Models.EventInfo
                    {
                        Id = "1",
                        Source = "Source",
                        Time = DateTime.Now,
                        Name = "Type"
                    });
                dbContext.Set<Models.EventInfo>()
                    .Add(new Models.EventInfo
                    {
                        Id = "2",
                        Source = "Source",
                        Time = DateTime.Now,
                        Name = "Type"
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

                dbContext.Set<Asset>().Add(
                    new Asset
                    {
                        Id = "1",
                        Tag = "Site 1",
                        Description = "Site 1",
                        Assets = new List<Asset>
                        {
                        new Asset
                        {
                            Id = "2",
                            ParentId = "1",
                            Description = "Drilling equipment and systems",
                            Tag = "3",
                            Assets = new List<Asset>
                            {
                                new Asset
                                {
                                    Id = "3",
                                    ParentId = "2",
                                    Description = "Mud supply",
                                    Tag = "325",
                                    Assets = new List<Asset>
                                    {
                                        new Asset
                                        {
                                            Id = "4",
                                            ParentId = "3",
                                            Tag = "325-G1",
                                            Description = "Mud pump no.1",
                                            Manufacturer = "Some producer",
                                            SerialNumber = "1234568790",
                                            TimeSeries = new List<TimeSeries> { tag1 }
                                        },
                                         new Asset
                                        {
                                            Id = "5",
                                            ParentId = "3",
                                            Description = "Mud pump no.2",
                                            Tag = "325-G2",
                                            Manufacturer = "Some other producer",
                                            SerialNumber = "0987654321",
                                            TimeSeries = new List<TimeSeries> { tag2 },
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
