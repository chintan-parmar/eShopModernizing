using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Models;
using eShopPorted.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace eShopPorted.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly CatalogDBContext _db;

        public CatalogService(CatalogDBContext db)
        {
            _db = db;
        }

        public async Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(
            int pageSize,
            int pageIndex,
            CancellationToken cancellationToken = default)
        {
            var totalItems = await _db.CatalogItems.LongCountAsync(cancellationToken);

            var itemsOnPage = await _db.CatalogItems
                .AsNoTracking()
                .Include(c => c.CatalogBrand)
                .Include(c => c.CatalogType)
                .OrderBy(c => c.Id)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedItemsViewModel<CatalogItem>(
                pageIndex, pageSize, totalItems, itemsOnPage);
        }

        public Task<CatalogItem> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default)
        {
            return _db.CatalogItems
                .AsNoTracking()
                .Include(c => c.CatalogBrand)
                .Include(c => c.CatalogType)
                .FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.CatalogTypes
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.CatalogBrands
                .AsNoTracking()
                .OrderBy(b => b.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
        {
            await _db.CatalogItems.AddAsync(catalogItem, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
        {
            _db.Entry(catalogItem).State = EntityState.Modified;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
        {
            _db.CatalogItems.Remove(catalogItem);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
