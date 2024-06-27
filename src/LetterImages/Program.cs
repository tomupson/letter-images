using LetterImages.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LetterImages;

internal sealed class Program
{
    private static readonly Dictionary<Colour, string> _colourDictionary = new();

    private static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No image path provided");
            Console.ReadKey();
            return;
        }

        string path = args[0];

        if (!File.Exists(path))
        {
            Console.WriteLine($"Couldn't find image at {path}");
            Console.ReadKey();
            return;
        }

        await BuildColourDictionary();

        byte[] imageBytes = await File.ReadAllBytesAsync(path);

        string[,] images;
        int newWidth;
        int newHeight;
        using (Image<Rgba32> image = Image.Load<Rgba32>(imageBytes))
        {
            newWidth = Math.Min(200, image.Width);
            float multiplier = 1 - Math.Abs(newWidth - image.Width) / (float)image.Width;
            newHeight = (int)Math.Floor(image.Height * multiplier);

            image.Mutate(img => img.Resize(newWidth, newHeight));

            images = FindMostSuitableImages(image);
        }

        Console.WriteLine("Step 3: Create output image. This may take a while.");

        const int letterSize = 10;
        int outputHeight = newHeight * letterSize;
        int outputWidth = newWidth * letterSize;

        Dictionary<string, byte[]> letterDictionary = new();

        using (Image<Rgba32> newImage = new Image<Rgba32>(outputWidth, outputHeight))
        {
            for (int x = 0; x < outputWidth; x += letterSize)
            {
                for (int y = 0; y < outputHeight; y += letterSize)
                {
                    string letterPath = images[x / letterSize, y / letterSize];
                    byte[] letterBytes;
                    if (letterDictionary.TryGetValue(letterPath, out byte[]? value))
                    {
                        letterBytes = value;
                    }
                    else
                    {
                        letterBytes = await File.ReadAllBytesAsync(letterPath);
                        letterDictionary.Add(letterPath, letterBytes);
                    }

                    using Image<Rgba32> letterImage = Image.Load<Rgba32>(letterBytes);
                    letterImage.Mutate(letterImg => letterImg.Resize(letterSize, letterSize));
                    for (int letterX = 0; letterX < letterSize; letterX++)
                    {
                        for (int letterY = 0; letterY < letterSize; letterY++)
                        {
                            newImage[letterX + x, letterY + y] = letterImage[letterX, letterY];
                        }
                    }
                }
            }

            using FileStream fileStream = File.Create("output.png");
            newImage.SaveAsPng(fileStream);
        }

        Console.WriteLine("Step 3: Complete");
        Console.WriteLine("Done!");
        Console.ReadKey();
    }

    /// <summary>
    /// Gets the average colour for every letter, and returns a dictionary mapping every colour to it's associated image
    /// </summary>
    /// <returns>A dictionary of colours to letter image paths</returns>
    private static async Task BuildColourDictionary()
    {
        Console.WriteLine("Step 1: Build colour dictionary");
        foreach (string filePath in Directory.EnumerateFiles("letters", "*", SearchOption.TopDirectoryOnly))
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(filePath);
            using Image<Rgba32> image = Image.Load<Rgba32>(imageBytes);
            int r = 0;
            int g = 0;
            int b = 0;
            int total = 0;

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Rgba32 pixel = image[x, y];
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    total++;
                }
            }

            r /= total;
            g /= total;
            b /= total;

            _colourDictionary.TryAdd(new Colour((byte)r, (byte)g, (byte)b), filePath);
        }

        Console.WriteLine("Step 1: Complete");
    }

    /// <summary>
    /// Analyses the specified image and chooses the most suitable letter images for each pixel
    /// </summary>
    /// <param name="image">The image to analyse</param>
    /// <returns>A 2D array of the letter image paths per pixel</returns>
    private static string[,] FindMostSuitableImages(Image<Rgba32> image)
    {
        Console.WriteLine("Step 2: Find must suitable images");
        string[,] images = new string[image.Width, image.Height];

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                Rgba32 pixel = image[x, y];
                images[x, y] = _colourDictionary[GetNearestColour(pixel.R, pixel.G, pixel.B)];
            }
        }

        Console.WriteLine("Step 2: Complete");

        return images;
    }

    private static Colour GetNearestColour(byte r, byte g, byte b)
    {
        Colour nearestColour = new Colour();
        double distance = 500.0;

        foreach (Colour colour in _colourDictionary.Keys)
        {
            double red = Math.Pow(colour.R - r, 2.0f);
            double green = Math.Pow(colour.G - g, 2.0f);
            double blue = Math.Pow(colour.B - b, 2.0f);
            double calculatedDistance = Math.Sqrt(red + green + blue);

            if (Math.Abs(calculatedDistance) < 0.001)
            {
                nearestColour = colour;
                break;
            }

            if (calculatedDistance >= distance) continue;

            distance = calculatedDistance;
            nearestColour = colour;
        }

        return nearestColour;
    }
}
