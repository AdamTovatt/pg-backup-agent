using System.Text.Json;

namespace PgBackupAgent.Configuration
{
    internal static class JsonOptions
    {
        internal static JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
