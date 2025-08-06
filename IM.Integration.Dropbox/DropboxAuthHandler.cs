using System.Net.Http.Headers;

namespace IM.Integration.Dropbox;

public class DropboxAuthHandler : DelegatingHandler
{
    private readonly IDropboxTokenValidator _tokenValidator;

    public DropboxAuthHandler(IDropboxTokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenValidator.EnsureValidToken(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
