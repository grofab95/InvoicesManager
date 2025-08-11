using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace IM.Tools;

public static class PdfCreator
{
    public static byte[] CreatePdfFromJpg(byte[] imageBytes)
    {
        // Load image (ImageSharp auto-detects format, no decoder needed)
        using var image = Image.Load<Rgba32>(imageBytes);

        // Prepare PDF document
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Width = image.Width;
        page.Height = image.Height;

        using var gfx = XGraphics.FromPdfPage(page);

        // Convert ImageSharp image to stream (JPG format)
        using var imageStream = new MemoryStream();
        image.SaveAsJpeg(imageStream); // Use ImageSharp's SaveAsJpeg
        imageStream.Seek(0, SeekOrigin.Begin);

        // Draw image to PDF
        using var xImage = XImage.FromStream(() => imageStream);
        gfx.DrawImage(xImage, 0, 0, image.Width, image.Height);

        // Return PDF as byte array
        using var outputStream = new MemoryStream();
        document.Save(outputStream);
        return outputStream.ToArray();
    }
}
