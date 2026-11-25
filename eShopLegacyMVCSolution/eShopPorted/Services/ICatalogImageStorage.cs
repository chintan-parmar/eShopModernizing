using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace eShopPorted.Services
{
    public sealed record CatalogImageResult(Stream Stream, string ContentType);

    public interface ICatalogImageStorage
    {
        Task<CatalogImageResult> GetImageAsync(string pictureFileName, CancellationToken cancellationToken = default);
    }
}
