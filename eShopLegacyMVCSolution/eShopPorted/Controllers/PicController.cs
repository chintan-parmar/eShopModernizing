using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Services;
using log4net;
using Microsoft.AspNetCore.Mvc;

namespace eShopPorted.Controllers
{
    public class PicController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string GetPicRouteName = "GetPicRouteTemplate";

        private readonly ICatalogService _service;
        private readonly ICatalogImageStorage _imageStorage;

        public PicController(ICatalogService service, ICatalogImageStorage imageStorage)
        {
            _service = service;
            _imageStorage = imageStorage;
        }

        // GET: items/5/pic
        [HttpGet("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public async Task<IActionResult> Index(int catalogItemId, CancellationToken cancellationToken)
        {
            _log.Info($"Now loading... /items/{catalogItemId}/pic");

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
