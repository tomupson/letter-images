using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ClosestColours
{
    internal class Program
    {
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

            Console.WriteLine("Creating image. This may take a while...");

            Dictionary<Colour, string> colourDictionary = await BuildColourDictionary();

            byte[] imageBytes = await File.ReadAllBytesAsync(path);
            using (Image<Rgba32> image = Image.Load(imageBytes))
            {
                int newWidth = Math.Min(250, image.Width);
                float multiplier = 1 - Math.Abs(newWidth - image.Width) / (float)image.Width;
                int newHeight = (int)Math.Floor(image.Height * multiplier);

                image.Mutate(img => img.Resize(newWidth, newHeight));

                string[,] images = new string[newWidth, newHeight];

                for (int x = 0; x < newWidth; x++)
                {
                    for (int y = 0; y < newHeight; y++)
                    {
                        Rgba32 pixel = image[x, y];
                        images[x, y] = colourDictionary[GetNearestColour(ref colourDictionary, pixel.R, pixel.G, pixel.B)];
                    }
                }

                const int letterSize = 10;
                int outputWidth = newWidth * letterSize;
                int outputHeight = newHeight * letterSize;
                using (Image<Rgba32> newImage = new Image<Rgba32>(outputWidth, outputHeight))
                {
                    for (int x = 0; x < outputWidth; x += letterSize)
                    {
                        for (int y = 0; y < outputHeight; y += letterSize)
                        {
                            byte[] letterImageBytes = await File.ReadAllBytesAsync(images[x / letterSize, y / letterSize]);
                            using (Image<Rgba32> letterImage = Image.Load(letterImageBytes))
                            {
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
                    }

                    using (FileStream fileStream = File.Create("output.png"))
                    {
                        newImage.SaveAsPng(fileStream);
                    }
                }

                Console.WriteLine("Done!");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Gets the average colour for every letter, and returns a dictionary mapping every colour to it's associated image
        /// </summary>
        /// <returns>A dictionary of colours to letter image paths</returns>
        private static async Task<Dictionary<Colour, string>> BuildColourDictionary()
        {
            Dictionary<Colour, string> colourDictionary = new Dictionary<Colour, string>();
            foreach (string filePath in Directory.EnumerateFiles("letters", "*", SearchOption.TopDirectoryOnly))
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(filePath);
                using (Image<Rgba32> image = Image.Load(imageBytes))
                {
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

                    colourDictionary.TryAdd(new Colour((byte)r, (byte)g, (byte)b), filePath);
                }
            }

            return colourDictionary;
        }

        private static Colour GetNearestColour(ref Dictionary<Colour, string> colourDictionary, byte r, byte g, byte b)
        {
            Colour nearestColour = new Colour();
            double distance = 500.0;

            foreach (Colour colour in colourDictionary.Keys)
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
}