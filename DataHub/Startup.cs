using DataHub.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;

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
            var dbContext = new LocalDBContext();

            services.AddScoped(
                typeof(IEntitiesRepository),
                sp => new EntitiesRepository());

            services.AddScoped(
                typeof(IBlobRepository),
                sp => new BlobRepository());

            services.AddScoped(
                typeof(DbContext),
                sp => dbContext);

            services.AddScoped(
                typeof(IQueryableRepository<Models.FileInfo>),
                sp => new EntityFrameworkRepository<Models.FileInfo>(dbContext));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1); ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
