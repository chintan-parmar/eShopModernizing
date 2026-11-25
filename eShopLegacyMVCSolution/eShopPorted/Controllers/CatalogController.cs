using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using eShopPorted.Models;
using eShopPorted.Services;
using log4net;

namespace eShopPorted.Controllers
{
    public class CatalogController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ICatalogService service;

        public CatalogController(ICatalogService service)
        {
            this.service = service;
        }

        // GET /[?pageSize=3&pageIndex=10]
        public ActionResult Index(int pageSize = 10, int pageIndex = 0)
        {
            _log.Info($"Now loading... /Catalog/Index?pageSize={pageSize}&pageIndex={pageIndex}");
            var paginatedItems = service.GetCatalogItemsPaginated(pageSize, pageIndex);
            ChangeUriPlaceholder(paginatedItems.Data);
            return View(paginatedItems);
        }

        // GET: Catalog/Details/5
        public ActionResult Details(int? id)
        {
            _log.Info($"Now loading... /Catalog/Details?id={id}");
            if (id == null)
            {
                return BadRequest();
            }
            CatalogItem catalogItem = service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }
            AddUriPlaceHolder(catalogItem);

            return View(catalogItem);
        }

        // GET: Catalog/Create
        public ActionResult Create()
        {
            _log.Info($"Now loading... /Catalog/Create");
            ViewBag.CatalogBrandId = new SelectList(service.GetCatalogBrands(), "Id", "Brand");
            ViewBag.CatalogTypeId = new SelectList(service.GetCatalogTypes(), "Id", "Type");
            return View(new CatalogItem());
        }

        // POST: Catalog/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind("Id,Name,Description,Price,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem)
        {
            _log.Info($"Now processing... /Catalog/Create?catalogItemName={catalogItem.Name}");
            catalogItem.PictureFileName = CatalogItem.DefaultPictureName;
            if (ModelState.IsValid)
            {
                service.CreateCatalogItem(catalogItem);
                return RedirectToAction("Index");
            }

            ViewBag.CatalogBrandId = new SelectList(service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new SelectList(service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            return View(catalogItem);
        }

        // GET: Catalog/Edit/5
        public ActionResult Edit(int? id)
        {
            _log.Info($"Now loading... /Catalog/Edit?id={id}");
            if (id == null)
            {
                return BadRequest();
            }
            CatalogItem catalogItem = service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }
            AddUriPlaceHolder(catalogItem);
            ViewBag.CatalogBrandId = new SelectList(service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new SelectList(service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            return View(catalogItem);
        }

        // POST: Catalog/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind("Id,Name,Description,Price,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem)
        {
            _log.Info($"Now processing... /Catalog/Edit?id={catalogItem.Id}");
            var existingItem = service.FindCatalogItem(catalogItem.Id);
            if (existingItem == null)
            {
                return NotFound();
            }

            catalogItem.PictureFileName = existingItem.PictureFileName;
            if (ModelState.IsValid)
            {
                service.UpdateCatalogItem(catalogItem);
                return RedirectToAction("Index");
            }
            AddUriPlaceHolder(catalogItem);
            ViewBag.CatalogBrandId = new SelectList(service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new SelectList(service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            return View(catalogItem);
        }

        // GET: Catalog/Delete/5
        public ActionResult Delete(int? id)
        {
            _log.Info($"Now loading... /Catalog/Delete?id={id}");
            if (id == null)
            {
                return BadRequest();
            }
            CatalogItem catalogItem = service.FindCatalogItem(id.Value);
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
        public ActionResult DeleteConfirmed(int id)
        {
            _log.Info($"Now processing... /Catalog/DeleteConfirmed?id={id}");
            CatalogItem catalogItem = service.FindCatalogItem(id);
            if (catalogItem != null)
            {
                service.RemoveCatalogItem(catalogItem);
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            _log.Debug($"Now disposing");
            if (disposing)
            {
                service.Dispose();
            }
            base.Dispose(disposing);
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
