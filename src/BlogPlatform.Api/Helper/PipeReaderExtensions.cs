using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace BlogPlatform.Api.Helper
{
    public static class PipeReaderExtensions
    {
        public static Task<T?> DeserializeJsonAsync<T>(this PipeReader reader, CancellationToken cancellationToken = default) => DeserializeAsync<T>(reader, null, cancellationToken);

        public static async Task<T?> DeserializeAsync<T>(this PipeReader reader, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default)
        {
            ReadResult json = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = json.Buffer;
            T? result = Deserialize<T>(ref buffer, serializerOptions);
            reader.AdvanceTo(buffer.End);
            return result;
        }

        private static T? Deserialize<T>(ref ReadOnlySequence<byte> buffer, JsonSerializerOptions? serializerOptions)
        {
            JsonReaderOptions jsonReaderOptions = new()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            };

            Utf8JsonReader utf8JsonReader = new(buffer, jsonReaderOptions);
            T? result = JsonSerializer.Deserialize<T>(ref utf8JsonReader, serializerOptions);
            buffer = buffer.Slice(utf8JsonReader.Position);
            return result;
        }
    }
}
