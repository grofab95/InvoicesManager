namespace IM.Integration.Dropbox;

public interface IDropboxTokenValidator
{
    Task<string> EnsureValidToken(CancellationToken cancellationToken);
}