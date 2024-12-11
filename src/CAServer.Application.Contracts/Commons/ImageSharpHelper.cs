using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;

namespace CAServer.Commons;

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ImageSharpHelper
{
    private const int TargetWidth = 50;
    private const int TargetHeight = 50;
    public static async Task<bool> SaveLogo(string url)
    {
        try
        {
            string outputPath = WebsiteInfoHelper.GetLogoUrlMd5(url);
            Console.WriteLine($"SaveLogo url = {url} outputPath = {outputPath}");

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(outputPath, imageBytes);
            }

            Image<Rgba32> image = Image.Load<Rgba32>(outputPath);
            image.Mutate(x => x.Resize(TargetWidth, TargetHeight));
            image.Save(outputPath);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"ImageSharpHelper SaveLogo {url} has error");
            Console.WriteLine(e);
            return false;
        }
    }
    

    public static bool CalculateGrayImageSimilarity(string imagePath1, string imagePath2)
    {
        using (Image<Rgba32> image1 = Image.Load<Rgba32>(imagePath1))
        using (Image<Rgba32> image2 = Image.Load<Rgba32>(imagePath2))
        {
            if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                throw new InvalidOperationException("Images must be the same size for comparison");
            }

            double totalDifference = 0;
            for (int y = 0; y < image1.Height; y++)
            {
                for (int x = 0; x < image1.Width; x++)
                {
                    var pixel1 = image1[x, y];
                    var pixel2 = image2[x, y];

                    double diff = Math.Sqrt(
                        Math.Pow(pixel1.R - pixel2.R, 2) +
                        Math.Pow(pixel1.G - pixel2.G, 2) +
                        Math.Pow(pixel1.B - pixel2.B, 2));

                    totalDifference += diff;
                }
            }

            double averageDifference = totalDifference / (image1.Width * image1.Height);
            Console.WriteLine("Average Color Difference: " + averageDifference);
            return averageDifference < 50;
        }
    }
}