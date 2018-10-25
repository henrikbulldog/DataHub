using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataHub.Middleware
{
    /// <summary>
    /// Transforms claims by adding permissions to the claims principal
    /// </summary>
    public class ClaimsTransformer : IClaimsTransformation
    {
        private IConfiguration configuration;
        private IHttpContextAccessor httpContextAccessor;

        public ClaimsTransformer(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
        }

        private string GetClaimValue(ClaimsIdentity identity, string claimType)
        {
            return identity
                .Claims
                .Where(c => c.Type == claimType)
                .Select(c => c.Value)
                .FirstOrDefault();
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return await Task.Run(() => Transform(principal));
        }

        private ClaimsPrincipal Transform(ClaimsPrincipal principal)
        {
            if (principal != null)
            {
                var identity = (ClaimsIdentity)principal.Identity;
                if (identity != null)
                {
                    var appid = GetClaimValue(identity, "appid");
                    var readerClientIds = configuration
                        .GetSection("AzureSecurityGroup:ReaderClientIds").Get<List<string>>();
                    if (readerClientIds.Contains(appid))
                    {
                        identity.AddClaim(new Claim(
                            "groups",
                            configuration.GetValue<string>("AzureSecurityGroup:DataHubReadersObjectID")));
                    }

                    var writerClientIds = configuration
                        .GetSection("AzureSecurityGroup:WriterClientIds").Get<List<string>>();
                    if (writerClientIds.Contains(appid))
                    {
                        identity.AddClaim(new Claim(
                            "groups",
                            configuration.GetValue<string>("AzureSecurityGroup:DataHubWritersObjectID")));
                    }
                }
            }

            return principal;
        }
    }
}
