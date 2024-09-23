using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Libraries.Clients.Common
{
    public class UserAuthService(IHttpClientFactory httpClientFactory, IOptions<UserAuthServiceOptions> options)
    {
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
        private readonly IOptions<UserAuthServiceOptions> options = options;

        private UserAuthServiceToken? Token { get; set; }
        //private SemaphoreSlim SemaphoreSlim { get; set; } = new SemaphoreSlim(1, 1);

        public async Task<string?> GetTokenAsync()
        {
            if (Token is null || Token.IsExpired)
            {
                using var client = httpClientFactory.CreateClient();
#if DEBUG
                string url = options.Value.URL_Debug;
#else
                string url = options.Value.URL;
#endif
                var response = await client.PostAsJsonAsync(url, new 
                { 
                    id = options.Value.User_id, 
                    password = options.Value.Password 
                });

                Token = await response.Content.ReadFromJsonAsync<UserAuthServiceToken>();
                return Token?.Access_token;
            }
            else
            {
                return Token.Access_token;
            }
        }

    }
}
