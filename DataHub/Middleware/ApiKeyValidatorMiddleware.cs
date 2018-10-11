using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Middleware
{
    public class ApiKeyValidatorMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyValidatorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.ToString().ToLower().Contains("swagger"))
            {
                if (!context.Request.Headers.Keys.Contains("api-key"))
                {
                    context.Response.StatusCode = 400; //Bad Request                
                    await context.Response.WriteAsync("API Key is missing");
                    return;
                }

                if (context.Request.Headers["api-key"] != Environment.GetEnvironmentVariable("DataHub.Api.Key"))
                {
                    context.Response.StatusCode = 401; //UnAuthorized
                    await context.Response.WriteAsync("Invalid API Key");
                    return;
                }
            }

            await _next.Invoke(context);
        }

    }
}
