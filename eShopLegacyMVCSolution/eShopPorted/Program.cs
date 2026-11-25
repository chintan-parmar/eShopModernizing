using System;
using System.Threading.Tasks;
using eShopPorted.Models;
using eShopPorted.Services;
using Google.Cloud.AspNetCore.DataProtection.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eShopPorted
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
            });

            var port = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(port))
            {
                builder.WebHost.UseSetting("http_ports", port);
            }

            builder.Logging.ClearProviders();
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddSimpleConsole();
            }
            else
            {
                builder.Logging.AddJsonConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    options.UseUtcTimestamp = true;
                });
            }

            string dataProtectionBucket = builder.Configuration["DataProtection:BucketName"];
            if (!string.IsNullOrWhiteSpace(dataProtectionBucket))
            {
                string dataProtectionObject = builder.Configuration["DataProtection:ObjectName"];
                if (string.IsNullOrWhiteSpace(dataProtectionObject))
                {
                    dataProtectionObject = "dataprotection/keys.xml";
                }

                builder.Services
                    .AddDataProtection()
                    .SetApplicationName("eShopCatalog")
                    .PersistKeysToGoogleCloudStorage(dataProtectionBucket, dataProtectionObject);
            }

            builder.Services.AddControllersWithViews();
            builder.Services.AddHealthChecks();

            builder.Services
                .AddAuthentication(CatalogAuthorizationPolicies.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, CloudRunIamAuthenticationHandler>(
                    CatalogAuthorizationPolicies.SchemeName,
                    options => { });

            builder.Services
                .AddAuthorizationBuilder()
                .AddPolicy(CatalogAuthorizationPolicies.CatalogWrite, policy =>
                    policy.RequireAuthenticatedUser());

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            bool useMockData = builder.Configuration.GetValue<bool>("UseMockData");
            string storageBucketName = builder.Configuration["Storage:BucketName"];
            if (!string.IsNullOrWhiteSpace(storageBucketName))
            {
                string objectPrefix = builder.Configuration["Storage:ObjectPrefix"];
                builder.Services.AddSingleton<ICatalogImageStorage>(_ =>
                    new GcsCatalogImageStorage(storageBucketName, objectPrefix));
            }
            else
            {
                builder.Services.AddSingleton<ICatalogImageStorage, LocalCatalogImageStorage>();
            }

            if (useMockData)
            {
                builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
            }
            else
            {
                string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "ConnectionStrings:DefaultConnection must be configured (e.g., via Secret Manager env var ConnectionStrings__DefaultConnection) when UseMockData is false.");
                }

                builder.Services.AddDbContext<CatalogDBContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null)));
                builder.Services.AddScoped<ICatalogService, CatalogService>();
            }

            var app = builder.Build();

            if (!useMockData && app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
                await db.Database.MigrateAsync();
            }

            app.UseForwardedHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Catalog/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/healthz");
            app.MapGet("/api", () => Results.Json(new { Message = "Hello World!" }));
            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Catalog}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
