using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Libraries.Web.Common.Http;

public class ForwardTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader))
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);

        return await base.SendAsync(request, ct);
    }
}
