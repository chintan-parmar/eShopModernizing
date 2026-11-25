using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Models;
using eShopPorted.Models.Infrastructure;
using eShopPorted.ViewModel;

namespace eShopPorted.Services
{
    public class CatalogServiceMock : ICatalogService
    {
        private readonly Lock _sync = new();
        private readonly List<CatalogItem> _catalogItems;
        private readonly IReadOnlyList<CatalogBrand> _catalogBrands;
        private readonly IReadOnlyList<CatalogType> _catalogTypes;

        public CatalogServiceMock()
        {
            _catalogBrands = PreconfiguredData.GetPreconfiguredCatalogBrands().ToList();
            _catalogTypes = PreconfiguredData.GetPreconfiguredCatalogTypes().ToList();
            _catalogItems = new List<CatalogItem>(PreconfiguredData.GetPreconfiguredCatalogItems());
        }

        public Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(
            int pageSize = 10,
            int pageIndex = 0,
            CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                var totalCount = _catalogItems.Count;
                var itemsOnPage = _catalogItems
                    .OrderBy(c => c.Id)
                    .Skip(pageSize * pageIndex)
                    .Take(pageSize)
                    .Select(CloneWithRelations)
                    .ToList();

                return Task.FromResult(new PaginatedItemsViewModel<CatalogItem>(
                    pageIndex, pageSize, totalCount, itemsOnPage));
            }
        }

        public Task<CatalogItem> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                var item = _catalogItems.FirstOrDefault(x => x.Id == id);
                return Task.FromResult(item == null ? null : CloneWithRelations(item));
            }
        }

        public Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_catalogTypes);
        }

        public Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_catalogBrands);
        }

        public Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                var nextId = _catalogItems.Count == 0 ? 1 : _catalogItems.Max(i => i.Id) + 1;
                catalogItem.Id = nextId;
                _catalogItems.Add(CloneItem(catalogItem));
            }

            return Task.CompletedTask;
        }

        public Task UpdateCatalogItemAsync(CatalogItem modifiedItem, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                var index = _catalogItems.FindIndex(x => x.Id == modifiedItem.Id);
                if (index >= 0)
                {
                    _catalogItems[index] = CloneItem(modifiedItem);
                }
            }

            return Task.CompletedTask;
        }

        public Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                _catalogItems.RemoveAll(x => x.Id == catalogItem.Id);
            }

            return Task.CompletedTask;
        }

        private CatalogItem CloneWithRelations(CatalogItem source)
        {
            var clone = CloneItem(source);
            clone.CatalogBrand = _catalogBrands.FirstOrDefault(b => b.Id == clone.CatalogBrandId);
            clone.CatalogType = _catalogTypes.FirstOrDefault(t => t.Id == clone.CatalogTypeId);
            return clone;
        }

        private static CatalogItem CloneItem(CatalogItem source)
        {
            return new CatalogItem
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Price = source.Price,
                PictureFileName = source.PictureFileName,
                PictureUri = source.PictureUri,
                CatalogBrandId = source.CatalogBrandId,
                CatalogTypeId = source.CatalogTypeId,
                AvailableStock = source.AvailableStock,
                RestockThreshold = source.RestockThreshold,
                MaxStockThreshold = source.MaxStockThreshold,
                OnReorder = source.OnReorder
            };
        }
    }
}
