using IM.Integration.Dropbox.Models;

namespace IM.Integration.Dropbox.Extensions;

public static class TokenDataExtensions
{
    public static bool IsExpired(this TokenData tokenData)
    {
        return tokenData.ExpiresAt <= DateTime.UtcNow.AddMinutes(10);
    }
}