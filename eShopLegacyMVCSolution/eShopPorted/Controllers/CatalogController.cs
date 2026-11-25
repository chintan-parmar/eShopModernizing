using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Models;
using eShopPorted.Services;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eShopPorted.Controllers
{
    public class CatalogController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICatalogService _service;

        public CatalogController(ICatalogService service)
        {
            _service = service;
        }

        // GET /[?pageSize=3&pageIndex=10]
        public async Task<IActionResult> Index(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
        {
            _log.Info($"Now loading... /Catalog/Index?pageSize={pageSize}&pageIndex={pageIndex}");
            var paginatedItems = await _service.GetCatalogItemsPaginatedAsync(pageSize, pageIndex, cancellationToken);
            ChangeUriPlaceholder(paginatedItems.Data);
            return View(paginatedItems);
        }

        // GET: Catalog/Details/5
        public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken = default)
        {
            _log.Info($"Now loading... /Catalog/Details?id={id}");
            if (id == null)
            {
                return BadRequest();
            }

            var catalogItem = await _service.FindCatalogItemAsync(id.Value, cancellationToken);
            if (catalogItem == null)
            {
                return NotFound();
            }

            AddUriPlaceHolder(catalogItem);
            return View(catalogItem);
        }

        // GET: Catalog/Create
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            _log.Info("Now loading... /Catalog/Create");
            await PopulateDropDownsAsync(null, null, cancellationToken);
            return View(new CatalogItem());
        }

        // POST: Catalog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,Description,Price,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem,
            CancellationToken cancellationToken = default)
        {
            _log.Info($"Now processing... /Catalog/Create?catalogItemName={catalogItem.Name}");
            catalogItem.PictureFileName = CatalogItem.DefaultPictureName;
            if (ModelState.IsValid)
            {
                await _service.CreateCatalogItemAsync(catalogItem, cancellationToken);
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownsAsync(catalogItem.CatalogBrandId, catalogItem.CatalogTypeId, cancellationToken);
            return View(catalogItem);
        }

        // GET: Catalog/Edit/5
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken = default)
        {
            _log.Info($"Now loading... /Catalog/Edit?id={id}");
            if (id == null)
            {
                return BadRequest();
            }

            var catalogItem = await _service.FindCatalogItemAsync(id.Value, cancellationToken);
            if (catalogItem == null)
            {
                return NotFound();
            }

            AddUriPlaceHolder(catalogItem);
            await PopulateDropDownsAsync(catalogItem.CatalogBrandId, catalogItem.CatalogTypeId, cancellationToken);
            return View(catalogItem);
        }

        // POST: Catalog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("Id,Name,Description,Price,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem,
            CancellationToken cancellationToken = default)
        {
            _log.Info($"Now processing... /Catalog/Edit?id={catalogItem.Id}");
            var existingItem = await _service.FindCatalogItemAsync(catalogItem.Id, cancellationToken);
            if (existingItem == null)
            {
                return NotFound();
            }

            catalogItem.PictureFileName = existingItem.PictureFileName;
            if (ModelState.IsValid)
            {
                await _service.UpdateCatalogItemAsync(catalogItem, cancellationToken);
                return RedirectToAction(nameof(Index));
            }

            AddUriPlaceHolder(catalogItem);
            await PopulateDropDownsAsync(catalogItem.CatalogBrandId, catalogItem.CatalogTypeId, cancellationToken);
            return View(catalogItem);
        }

        // GET: Catalog/Delete/5
        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken = default)
        {
            _log.Info($"Now loading... /Catalog/Delete?id={id}");
            if (id == null)
            {
                return BadRequest();
            }

            var catalogItem = await _service.FindCatalogItemAsync(id.Value, cancellationToken);
            if (catalogItem == null)
            {
                return NotFound();
            }

            AddUriPlaceHolder(catalogItem);
            return View(catalogItem);
        }

        // POST: Catalog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
        {
            _log.Info($"Now processing... /Catalog/DeleteConfirmed?id={id}");
            var catalogItem = await _service.FindCatalogItemAsync(id, cancellationToken);
            if (catalogItem != null)
            {
                await _service.RemoveCatalogItemAsync(catalogItem, cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        private async Task PopulateDropDownsAsync(int? selectedBrandId, int? selectedTypeId, CancellationToken cancellationToken)
        {
            var brands = await _service.GetCatalogBrandsAsync(cancellationToken);
            var types = await _service.GetCatalogTypesAsync(cancellationToken);
            ViewBag.CatalogBrandId = new SelectList(brands, "Id", "Brand", selectedBrandId);
            ViewBag.CatalogTypeId = new SelectList(types, "Id", "Type", selectedTypeId);
        }

        private void ChangeUriPlaceholder(IEnumerable<CatalogItem> items)
        {
            foreach (var catalogItem in items)
            {
                AddUriPlaceHolder(catalogItem);
            }
        }

        private void AddUriPlaceHolder(CatalogItem item)
        {
            item.PictureUri = Url.RouteUrl(PicController.GetPicRouteName, new { catalogItemId = item.Id })
                ?? $"/items/{item.Id}/pic";
        }
    }
}
