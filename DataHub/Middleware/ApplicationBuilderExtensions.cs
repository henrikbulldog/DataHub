using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Middleware
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder ApplyUserKeyValidation(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiKeyValidatorMiddleware>();
            return app;
        }
    }
}
