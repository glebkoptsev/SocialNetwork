using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using UserService.Database.Entities;

namespace Libraries.Web.Common.Clients
{
    public class UserServiceClient(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        private readonly IHttpClientFactory _httpClientFactory = clientFactory;
#if DEBUG 
        private readonly string _url = configuration["UserService:URL_Debug"]!;
#else
        private readonly string _url = configuration["UserService:URL"]!;
#endif

        public async Task<User?> GetUserAsync(Guid id)
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_url}/api/user/get/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<User>();
        }
    }
}
