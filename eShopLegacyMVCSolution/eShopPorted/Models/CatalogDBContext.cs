using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eShopPorted.Models
{
    public class CatalogDBContext : DbContext
    {
        public CatalogDBContext(DbContextOptions<CatalogDBContext> options) : base(options)
        {
        }

        public DbSet<CatalogItem> CatalogItems { get; set; }
        public DbSet<CatalogBrand> CatalogBrands { get; set; }
        public DbSet<CatalogType> CatalogTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasSequence("catalog_hilo")
                .StartsAt(100)
                .IncrementsBy(10);

            builder.HasSequence("catalog_brand_hilo")
                .StartsAt(100)
                .IncrementsBy(10);

            builder.HasSequence("catalog_type_hilo")
                .StartsAt(100)
                .IncrementsBy(10);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(builder);
        }
    }

    public class CatalogDBContextDesignFactory : IDesignTimeDbContextFactory<CatalogDBContext>
    {
        public CatalogDBContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=tcp:localhost,1433;Database=CatalogDb;Encrypt=True;TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<CatalogDBContext>()
                .UseSqlServer(connectionString, sqlOptions =>
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null));

            return new CatalogDBContext(optionsBuilder.Options);
        }
    }
}
