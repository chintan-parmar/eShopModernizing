using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;

namespace eShopPorted.Services
{
    public static class CatalogImageFileNameValidator
    {
        private static readonly Regex SafeFileNamePattern = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        public static bool TrySanitize(string rawFileName, out string safeFileName)
        {
            safeFileName = string.Empty;
            if (string.IsNullOrWhiteSpace(rawFileName))
            {
                return false;
            }

            if (rawFileName.Contains("..", StringComparison.Ordinal) ||
                rawFileName.Contains('/') ||
                rawFileName.Contains('\\') ||
                !SafeFileNamePattern.IsMatch(rawFileName))
            {
                return false;
            }

            var fileNameOnly = Path.GetFileName(rawFileName);
            if (!string.Equals(fileNameOnly, rawFileName, StringComparison.Ordinal))
            {
                return false;
            }

            safeFileName = fileNameOnly;
            return true;
        }

        public static string ResolveContentType(string fileName)
        {
            return ContentTypeProvider.TryGetContentType(fileName, out var contentType)
                ? contentType
                : "application/octet-stream";
        }
    }

    public sealed class LocalCatalogImageStorage : ICatalogImageStorage
    {
        private readonly string _picsRootFull;
        private readonly string _picsRootPrefix;

        public LocalCatalogImageStorage(IWebHostEnvironment env)
        {
            _picsRootFull = Path.GetFullPath(Path.Combine(env.WebRootPath, "Pics"));
            _picsRootPrefix = _picsRootFull.EndsWith(Path.DirectorySeparatorChar)
                ? _picsRootFull
                : _picsRootFull + Path.DirectorySeparatorChar;
        }

        public Task<CatalogImageResult> GetImageAsync(string pictureFileName, CancellationToken cancellationToken = default)
        {
            if (!CatalogImageFileNameValidator.TrySanitize(pictureFileName, out var safeFileName))
            {
                return Task.FromResult<CatalogImageResult>(null);
            }

            var candidatePath = Path.GetFullPath(Path.Combine(_picsRootFull, safeFileName));
            if (!candidatePath.StartsWith(_picsRootPrefix, StringComparison.Ordinal) || !File.Exists(candidatePath))
            {
                return Task.FromResult<CatalogImageResult>(null);
            }

            var stream = new FileStream(
                candidatePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16384,
                useAsync: true);

            var contentType = CatalogImageFileNameValidator.ResolveContentType(safeFileName);
            return Task.FromResult(new CatalogImageResult(stream, contentType));
        }
    }

    public sealed class GcsCatalogImageStorage : ICatalogImageStorage, IDisposable
    {
        private readonly string _bucketName;
        private readonly string _objectPrefix;
        private readonly SemaphoreSlim _clientInitLock = new(1, 1);
        private StorageClient _storageClient;

        public GcsCatalogImageStorage(string bucketName, string objectPrefix)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _objectPrefix = string.IsNullOrWhiteSpace(objectPrefix)
                ? string.Empty
                : objectPrefix.Trim('/') + "/";
        }

        public async Task<CatalogImageResult> GetImageAsync(string pictureFileName, CancellationToken cancellationToken = default)
        {
            if (!CatalogImageFileNameValidator.TrySanitize(pictureFileName, out var safeFileName))
            {
                return null;
            }

            var client = await GetClientAsync(cancellationToken);
            var objectName = _objectPrefix + safeFileName;
            var memoryStream = new MemoryStream();

            try
            {
                var obj = await client.DownloadObjectAsync(
                    _bucketName,
                    objectName,
                    memoryStream,
                    cancellationToken: cancellationToken);

                memoryStream.Position = 0;
                var contentType = !string.IsNullOrWhiteSpace(obj?.ContentType)
                    ? obj.ContentType
                    : CatalogImageFileNameValidator.ResolveContentType(safeFileName);

                return new CatalogImageResult(memoryStream, contentType);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                await memoryStream.DisposeAsync();
                return null;
            }
            catch
            {
                await memoryStream.DisposeAsync();
                throw;
            }
        }

        private async Task<StorageClient> GetClientAsync(CancellationToken cancellationToken)
        {
            if (_storageClient != null)
            {
                return _storageClient;
            }

            await _clientInitLock.WaitAsync(cancellationToken);
            try
            {
                _storageClient ??= await StorageClient.CreateAsync();
                return _storageClient;
            }
            finally
            {
                _clientInitLock.Release();
            }
        }

        public void Dispose()
        {
            _storageClient?.Dispose();
            _clientInitLock.Dispose();
        }
    }
}
