using DataHub.Entities;
using DataHub.Hubs;
using DataHub.Middleware;
using DataHub.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RepositoryFramework.Azure.Blob;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;
using RepositoryFramework.Timeseries.InfluxDB;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataHub
{
    public class Startup
    {
        public const string EVENT_HUB_PATH = "/events-hub";
        public const string TIMESERIES_HUB_PATH = "/timeseries-hub";
        private ILogger logger;
        private IConfiguration configuration;


        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            logger = loggerFactory.CreateLogger<Startup>();
        }

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
                    @"Server=localhost;Database=NanoDataHub;Trusted_Connection=True;ConnectRetryCount=0",
                    //@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0",
#endif
                    sp.GetService<IEntitiesRepository>(),
                    logger);
                    return context;
                });

            services.AddScoped(
                typeof(IQueryableRepository<Entities.FileInfo>),
                sp => new EntityFrameworkRepository<Entities.FileInfo>(sp.GetService<EntitiesDBContext>()));

            services.AddScoped(
                typeof(IQueryableRepository<EventInfo>),
                sp => new EntityFrameworkRepository<EventInfo>(sp.GetService<EntitiesDBContext>()));

#if RELEASE
            services.AddScoped(
                typeof(IBlobRepository),
                sp => new AzureBlobRepository(CloudStorageAccount
                    .Parse(Environment.GetEnvironmentVariable("Azure.Storage.Connection"))
                    .CreateCloudBlobClient()
                    .GetContainerReference(configuration["Azure.Blob:Container"])));
#else   
            services.AddScoped(
                typeof(IBlobRepository),
                sp => new BlobRepository());
#endif

#if RELEASE
            services.AddSingleton(
                typeof(ITimeseriesRepository),
                sp => new InfluxDBRepository(
                    configuration["InfluxDB:Uri"],
                    configuration["InfluxDB:Database"],
                    configuration["InfluxDB:Measurement"],
                    Environment.GetEnvironmentVariable("InfluxDB.Username"),
                    Environment.GetEnvironmentVariable("InfluxDB.Password"),
                    (message, exception) => logger.LogError(message, exception)));
#else
            services.AddSingleton(
                typeof(ITimeseriesRepository),
                sp => new InfluxDBRepository("http://localhost:8086",
                    configuration["InfluxDB:Database"],
                    configuration["InfluxDB:Measurement"],
                    null,
                    null,
                    (message, exception) => logger.LogError(message, exception)));
#endif

            services.AddScoped(
                typeof(IEntityFrameworkRepository<TimeseriesMetadata>),
                sp => new EntityFrameworkRepository<TimeseriesMetadata>(sp.GetService<EntitiesDBContext>()));

#if RELEASE
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddAzureAdBearer(options => configuration.Bind("AzureAd", options));
#endif
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

            services.AddApplicationInsightsTelemetry();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Data Hub", Version = "v1" });
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "DataHub.xml");
                c.IncludeXmlComments(filePath);
                c.OperationFilter<FileOperationFilter>();
                c.CustomSchemaIds(type => type.FriendlyId().Replace("[", "Of").Replace("]", ""));
#if RELEASE
                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = configuration["AzureAd:Instance"] + configuration["AzureAd:TenantId"] + "/oauth2/authorize"
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "oauth2", new[] { "dummy" } }
                });
#endif
            });

            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithOrigins("http://localhost:4200", "https://datahub-client.azurewebsites.net");
            }));

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
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
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseCors("CorsPolicy");

            app.UseSignalR(routes =>
            {
                routes.MapHub<EventHub>(EVENT_HUB_PATH);
                routes.MapHub<TimeseriesHub>(TIMESERIES_HUB_PATH);
            });

#if RELEASE
            app.UseAuthentication();
#endif

#if RELEASE
            loggerFactory.AddApplicationInsights(app.ApplicationServices, Microsoft.Extensions.Logging.LogLevel.Warning);
#else
            loggerFactory.AddConsole();
#endif

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Data Hub");
#if RELEASE
                c.OAuthClientId(configuration["AzureAd:ClientId"]);
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>()
                    {
                       { "resource", configuration["AzureAd:ClientId"] }
                    });
#endif
            });
        }

        private void Seed(EntitiesDBContext dbContext)
        {
            if (dbContext.Set<Asset>().Count() == 0)
            {
                var timeseriesMetadata1 = new TimeseriesMetadata
                {
                    Source = "Seed",
                    Id = "1",
                    Name = "ABC123",
                    Description = "Some tag",
                    Units = "Pa"
                };
                var timeseriesMetadata2 = new TimeseriesMetadata
                {
                    Source = "Seed",
                    Id = "2",
                    Name = "ABC234",
                    Description = "Some other tag",
                    Units = "Pa"
                };
                dbContext.Set<TimeseriesMetadata>().Add(timeseriesMetadata1);
                dbContext.Set<TimeseriesMetadata>().Add(timeseriesMetadata2);
                dbContext.Set<TimeseriesMetadata>().Add(new TimeseriesMetadata
                {
                    Source = "Seed",
                    Id = "3",
                    Name = "Orphan"
                });
                dbContext.Set<TimeseriesMetadata>().Add(new TimeseriesMetadata
                {
                    Source = "Seed",
                    Id = "4",
                    Name = "Parent",
                    Children = new List<TimeseriesMetadata>
                    {
                        new TimeseriesMetadata
                        {
                            Source = "Seed",
                            Id = "5",
                            Name = "Child"
                        }
                    }
                });

                dbContext.Set<Entities.EventInfo>()
                    .Add(new Entities.EventInfo
                    {
                        Id = "1",
                        Source = "Seed",
                        Time = DateTime.Now,
                        Name = "Type"
                    });
                dbContext.Set<Entities.EventInfo>()
                    .Add(new Entities.EventInfo
                    {
                        Id = "2",
                        Source = "Seed",
                        Time = DateTime.Now,
                        Name = "Type"
                    });

                var file1 = new Entities.FileInfo
                {
                    Source = "Seed",
                    Id = "1",
                    Format = "CSV",
                    Filename = "ABC123.CSV"
                };
                dbContext.Set<Entities.FileInfo>().Add(file1);
                var file2 = new Entities.FileInfo
                {
                    Source = "Seed",
                    Id = "2",
                    Format = "CSV",
                    Filename = "ABC124.CSV"
                };
                dbContext.Set<Entities.FileInfo>().Add(file2);

                dbContext.Set<Asset>().Add(
                    new Asset
                    {
                        Source = "Seed",
                        Id = "1",
                        Tag = "Site 1",
                        Description = "Site 1",
                        Files = new List<Entities.FileInfo> { file1 },
                        Tags = new List<AssetTag>
                        {
                            new AssetTag
                            {
                                Source = "Seed",
                                Id = "1",
                                Name = "key1",
                                Value = "value1"
                            },
                            new AssetTag
                            {
                                Source = "Seed",
                                Id = "2",
                                Name = "key2",
                                Value = "value3"
                            }
                        },
                        TimeSeries = new List<TimeseriesMetadata> { timeseriesMetadata1 },
                        Children = new List<Asset>
                        {
                            new Asset
                            {
                                Source = "Seed",
                                Id = "2",
                                Description = "Drilling equipment and systems",
                                Tag = "3",
                                Children = new List<Asset>
                                {
                                    new Asset
                                    {
                                        Source = "Seed",
                                        Id = "3",
                                        Description = "Mud supply",
                                        Tag = "325",
                                        Children = new List<Asset>
                                        {
                                            new Asset
                                            {
                                                Source = "Seed",
                                                Id = "4",
                                                Tag = "325-G1",
                                                Description = "Mud pump no.1",
                                                Manufacturer = "Some producer",
                                                SerialNumber = "1234568790",
                                                TimeSeries = new List<TimeseriesMetadata> { timeseriesMetadata1 }
                                            },
                                             new Asset
                                            {
                                                Source = "Seed",
                                                Id = "5",
                                                Description = "Mud pump no.2",
                                                Tag = "325-G2",
                                                Manufacturer = "Some other producer",
                                                SerialNumber = "0987654321",
                                                TimeSeries = new List<TimeseriesMetadata> { timeseriesMetadata2 },
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
