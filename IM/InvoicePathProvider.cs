using IM.Core.Interfaces;

namespace IM;

public class InvoicePathProvider : IPathProvider
{
    public string GetDirectoryPath()
    {
        var now = DateTime.Now;
        var year = now.Year.ToString();
        var monthNumber = now.Month.ToString("00"); 
        var monthYear = $"{monthNumber}.{year}";
            
        return $"/Faktury/{year}/{monthYear}/Kosztowe";
    }
}

