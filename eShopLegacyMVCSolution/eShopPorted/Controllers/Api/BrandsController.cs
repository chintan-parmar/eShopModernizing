using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eShopPorted.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eShopPorted.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandsController : ControllerBase
    {
        private readonly ICatalogService _service;

        public BrandsController(ICatalogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var brands = await _service.GetCatalogBrandsAsync(cancellationToken);
            return Ok(brands);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            var brands = await _service.GetCatalogBrandsAsync(cancellationToken);
            var brand = brands.FirstOrDefault(x => x.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return Ok(brand);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = CatalogAuthorizationPolicies.CatalogWrite)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var brands = await _service.GetCatalogBrandsAsync(cancellationToken);
            var brandToDelete = brands.FirstOrDefault(x => x.Id == id);
            if (brandToDelete == null)
            {
                return NotFound();
            }

            // demo only - don't actually delete
            return Ok();
        }
    }
}
