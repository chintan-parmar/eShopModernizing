using System;
using System.Threading.Tasks;
using eShopPorted.Models;
using eShopPorted.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShopPorted
{
    public class Program
    {
        public static DateTime StartTime { get; } = DateTime.UtcNow;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var port = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(port))
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            }

            builder.Services.AddControllersWithViews();
            builder.Services.AddHealthChecks();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            bool useMockData = builder.Configuration.GetValue<bool>("UseMockData");
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
