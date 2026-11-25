using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Models;
using eShopPorted.ViewModel;

namespace eShopPorted.Services
{
    public interface ICatalogService
    {
        Task<CatalogItem> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default);
        Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default);
        Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);
        Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);
        Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);
    }
}
