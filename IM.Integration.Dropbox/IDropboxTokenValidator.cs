using IM.Core.Interfaces;

namespace IM.Integration.Dropbox;

public interface IDropboxTokenValidator : IInit
{
    Task<string> EnsureValidToken(CancellationToken cancellationToken);
}