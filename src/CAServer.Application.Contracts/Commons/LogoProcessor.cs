namespace CAServer.Commons;

using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class LogoProcessor
{
    private const int TargetWidth = 50;
    private const int TargetHeight = 50;

    public static double CalculateGrayImageSimilarity(string imagePath1, string imagePath2)
    {
        using var bitmap1 = SKBitmap.Decode(imagePath1);
        using var bitmap2 = SKBitmap.Decode(imagePath2);

        // Simple check to ensure bitmaps successfully loaded
        if (bitmap1 == null || bitmap2 == null)
        {
            throw new Exception("Image could not be loaded.");
        }

        // Resize images
        var resized1 = bitmap1.Resize(new SKImageInfo(TargetWidth, TargetHeight), SKFilterQuality.High);
        var resized2 = bitmap2.Resize(new SKImageInfo(TargetWidth, TargetHeight), SKFilterQuality.High);

        if (resized1 == null || resized2 == null)
        {
            throw new Exception("Failed to resize images.");
        }

        long totalDifference = 0;
        int pixelsCount = TargetWidth * TargetHeight;

        // Convert to grayscale and compare
        for (int y = 0; y < TargetHeight; y++)
        {
            for (int x = 0; x < TargetWidth; x++)
            {
                // Convert each pixel to grayscale
                var pixel1 = resized1.GetPixel(x, y);
                var pixel2 = resized2.GetPixel(x, y);

                // Using the luminosity method to calculate grayscale
                byte gray1 = (byte)(0.3 * pixel1.Red + 0.59 * pixel1.Green + 0.11 * pixel1.Blue);
                byte gray2 = (byte)(0.3 * pixel2.Red + 0.59 * pixel2.Green + 0.11 * pixel2.Blue);

                // Calculate the difference
                totalDifference += Math.Abs(gray1 - gray2);
            }
        }

        // Compute average pixel difference
        double avgDifference = totalDifference / (double)pixelsCount / 255.0;

        // Normalize to obtain similarity
        double similarity = 1.0 - avgDifference;

        return similarity;
    }


    public static async Task<bool> SaveLogo(string url)
    {
        try
        {
            byte[] imageData = await DownloadImageDataAsync(url);
            using (var inputStream = new SKManagedStream(new MemoryStream(imageData)))
            using (var input = SKBitmap.Decode(inputStream))
            {
                var newImage = input.Resize(new SKImageInfo(TargetWidth, TargetHeight), SKFilterQuality.High);
                using (var image = SKImage.FromBitmap(newImage))
                using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 100))
                using (var stream = File.OpenWrite(WebsiteInfoHelper.GetLogoUrlMd5(url)))
                {
                    data.SaveTo(stream);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"SaveLogo {url} has error");
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private static async Task<byte[]> DownloadImageDataAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetByteArrayAsync(url);
        }
    }
}