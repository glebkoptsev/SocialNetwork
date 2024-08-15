using System.Text.Json;

namespace UserService.API
{
    public static class Consts
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);
    }
}
