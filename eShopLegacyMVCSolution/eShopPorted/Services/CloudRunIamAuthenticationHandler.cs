using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eShopPorted.Services
{
    public static class CatalogAuthorizationPolicies
    {
        public const string SchemeName = "CloudRunIam";
        public const string CatalogWrite = "CatalogWrite";
    }

    public sealed class CloudRunIamAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public CloudRunIamAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration configuration)
            : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // For the Cloud Run POC, ingress access control is enforced by Cloud Run IAM (invoker_members).
            // Outside Cloud Run IAM (or when TrustCloudRunIam is false), mutating actions fail closed (401).
            bool trustCloudRunIam = _configuration.GetValue<bool>("Authentication:TrustCloudRunIam");
            if (!trustCloudRunIam)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "cloudrun-iam-caller")
            };
            var identity = new ClaimsIdentity(claims, CatalogAuthorizationPolicies.SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, CatalogAuthorizationPolicies.SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
