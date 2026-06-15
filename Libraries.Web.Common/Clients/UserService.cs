using Libraries.Web.Common.DTOs;
using System.Net.Http.Json;

namespace Libraries.Web.Common.Clients;

public class UserServiceClient(HttpClient client)
{
    public async Task<UserDto?> GetUserAsync(Guid id)
    {
        var response = await client.GetAsync($"/api/user/get/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<bool> GetSubscriptionStatusAsync(Guid friendId)
    {
        var response = await client.GetAsync($"/api/friend/status/{friendId}");
        if (!response.IsSuccessStatusCode) return false;
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
