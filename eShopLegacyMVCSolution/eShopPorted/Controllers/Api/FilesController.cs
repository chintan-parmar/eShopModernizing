using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eShopLegacy.Utilities;
using eShopPorted.Services;
using Microsoft.AspNetCore.Mvc;

namespace eShopPorted.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ICatalogService _service;

        public FilesController(ICatalogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var brands = (await _service.GetCatalogBrandsAsync(cancellationToken))
                .Select(b => new BrandDTO
                {
                    Id = b.Id,
                    Brand = b.Brand
                }).ToList();

            var serializer = new Serializing();
            var data = await serializer.SerializeJsonAsync(brands, cancellationToken);

            return File(data, "application/json");
        }

        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}
