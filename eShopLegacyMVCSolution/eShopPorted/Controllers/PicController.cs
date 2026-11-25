using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace eShopPorted.Controllers
{
    public class PicController : Controller
    {
        public const string GetPicRouteName = "GetPicRouteTemplate";

        private readonly ICatalogService _service;
        private readonly ICatalogImageStorage _imageStorage;
        private readonly ILogger<PicController> _logger;

        public PicController(
            ICatalogService service,
            ICatalogImageStorage imageStorage,
            ILogger<PicController> logger)
        {
            _service = service;
            _imageStorage = imageStorage;
            _logger = logger;
        }

        // GET: items/5/pic
        [HttpGet("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public async Task<IActionResult> Index(int catalogItemId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading picture for catalog item {CatalogItemId}", catalogItemId);

            if (catalogItemId <= 0)
            {
                return BadRequest();
            }

            var item = await _service.FindCatalogItemAsync(catalogItemId, cancellationToken);
            if (item == null)
            {
                return NotFound();
            }

            var image = await _imageStorage.GetImageAsync(item.PictureFileName, cancellationToken);
            if (image == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "public,max-age=3600";
            return File(image.Stream, image.ContentType);
        }
    }
}
