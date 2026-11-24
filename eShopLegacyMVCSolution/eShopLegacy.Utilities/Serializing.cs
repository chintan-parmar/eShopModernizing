using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace eShopLegacy.Utilities
{
    public class Serializing
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public Stream SerializeJson<T>(T input)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(input, SerializerOptions);
            return new MemoryStream(bytes, writable: false);
        }

        public async Task<MemoryStream> SerializeJsonAsync<T>(T input, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, input, SerializerOptions, cancellationToken);
            stream.Position = 0;
            return stream;
        }

        public ValueTask<T?> DeserializeJsonAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            return JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
        }
    }
}
