using SkiaSharp;

namespace PencariApi.Services;

public static class OpenCvFaceService
{
    public static double CalculateBestSimilarity(
        string capturedPhotoPath,
        IEnumerable<string> databasePhotoPaths)
    {
        try
        {
            if (!IsValidImageFile(capturedPhotoPath))
                return 0;

            if (!HasPossibleFace(capturedPhotoPath))
                return 0;

            if (databasePhotoPaths == null)
                return 0;

            double bestSimilarity = 0;

            foreach (string databasePhotoPath in databasePhotoPaths)
            {
                if (!IsValidImageFile(databasePhotoPath))
                    continue;

                if (!HasPossibleFace(databasePhotoPath))
                    continue;

                double similarity = CalculateSimilarity(capturedPhotoPath, databasePhotoPath);

                if (similarity > bestSimilarity)
                    bestSimilarity = similarity;
            }

            return bestSimilarity;
        }
        catch
        {
            return 0;
        }
    }

    public static double CalculateSimilarity(string capturedPhotoPath, string databasePhotoPath)
    {
        try
        {
            if (!IsValidImageFile(capturedPhotoPath) || !IsValidImageFile(databasePhotoPath))
                return 0;

            using SKBitmap? capturedOriginal = SKBitmap.Decode(capturedPhotoPath);
            using SKBitmap? databaseOriginal = SKBitmap.Decode(databasePhotoPath);

            if (capturedOriginal == null || databaseOriginal == null)
                return 0;

            using SKBitmap? capturedBitmap = ResizeBitmap(capturedOriginal, 96, 96);
            using SKBitmap? databaseBitmap = ResizeBitmap(databaseOriginal, 96, 96);

            if (capturedBitmap == null || databaseBitmap == null)
                return 0;

            double graySimilarity = CalculateGraySimilarity(capturedBitmap, databaseBitmap);
            double histogramSimilarity = CalculateHistogramSimilarity(capturedBitmap, databaseBitmap);

            double finalSimilarity = (graySimilarity * 0.45) + (histogramSimilarity * 0.55);

            if (finalSimilarity < 0)
                finalSimilarity = 0;

            if (finalSimilarity > 100)
                finalSimilarity = 100;

            return finalSimilarity;
        }
        catch
        {
            return 0;
        }
    }

    public static int CountValidFaceSamples(IEnumerable<string> databasePhotoPaths)
    {
        try
        {
            if (databasePhotoPaths == null)
                return 0;

            int count = 0;

            foreach (string path in databasePhotoPaths)
            {
                if (IsValidImageFile(path) && HasPossibleFace(path))
                    count++;
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }

    public static bool HasPossibleFace(string imagePath)
    {
        try
        {
            if (!IsValidImageFile(imagePath))
                return false;

            using SKBitmap? original = SKBitmap.Decode(imagePath);

            if (original == null)
                return false;

            using SKBitmap? bitmap = ResizeBitmap(original, 160, 120);

            if (bitmap == null)
                return false;

            int skinPixels = 0;
            int validPixels = 0;

            int startX = 35;
            int endX = 125;
            int startY = 15;
            int endY = 110;

            int minGray = 255;
            int maxGray = 0;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    SKColor color = bitmap.GetPixel(x, y);

                    int r = color.Red;
                    int g = color.Green;
                    int b = color.Blue;

                    int gray = ToGray(color);

                    if (gray < minGray)
                        minGray = gray;

                    if (gray > maxGray)
                        maxGray = gray;

                    if (IsSkinLikePixel(r, g, b))
                        skinPixels++;

                    validPixels++;
                }
            }

            if (validPixels == 0)
                return false;

            double skinRatio = skinPixels / (double)validPixels;
            int contrastRange = maxGray - minGray;

            if (skinRatio < 0.02)
                return false;

            if (contrastRange < 10)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSkinLikePixel(int r, int g, int b)
    {
        bool rule1 =
            r > 55 &&
            g > 30 &&
            b > 15 &&
            r > b &&
            Math.Abs(r - g) > 5;

        bool rule2 =
            r > 80 &&
            g > 45 &&
            b > 25 &&
            r >= g &&
            g >= b;

        bool rule3 =
            r > 70 &&
            g > 40 &&
            b > 20 &&
            r > g &&
            r > b;

        return rule1 || rule2 || rule3;
    }

    private static double CalculateGraySimilarity(SKBitmap bitmap1, SKBitmap bitmap2)
    {
        double totalDifference = 0;
        int totalPixels = bitmap1.Width * bitmap1.Height;

        for (int y = 0; y < bitmap1.Height; y++)
        {
            for (int x = 0; x < bitmap1.Width; x++)
            {
                int gray1 = ToGray(bitmap1.GetPixel(x, y));
                int gray2 = ToGray(bitmap2.GetPixel(x, y));

                totalDifference += Math.Abs(gray1 - gray2);
            }
        }

        double averageDifference = totalDifference / totalPixels;
        double similarity = 100.0 - ((averageDifference / 255.0) * 100.0);

        if (similarity < 0)
            similarity = 0;

        if (similarity > 100)
            similarity = 100;

        return similarity;
    }

    private static double CalculateHistogramSimilarity(SKBitmap bitmap1, SKBitmap bitmap2)
    {
        int[] histogram1 = BuildGrayHistogram(bitmap1);
        int[] histogram2 = BuildGrayHistogram(bitmap2);

        int totalPixels = bitmap1.Width * bitmap1.Height;

        double difference = 0;

        for (int i = 0; i < histogram1.Length; i++)
        {
            double p1 = histogram1[i] / (double)totalPixels;
            double p2 = histogram2[i] / (double)totalPixels;

            difference += Math.Abs(p1 - p2);
        }

        double similarity = 100.0 - (difference * 50.0);

        if (similarity < 0)
            similarity = 0;

        if (similarity > 100)
            similarity = 100;

        return similarity;
    }

    private static int[] BuildGrayHistogram(SKBitmap bitmap)
    {
        int[] histogram = new int[16];

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                int gray = ToGray(bitmap.GetPixel(x, y));
                int bin = gray / 16;

                if (bin < 0)
                    bin = 0;

                if (bin > 15)
                    bin = 15;

                histogram[bin]++;
            }
        }

        return histogram;
    }

    private static SKBitmap? ResizeBitmap(SKBitmap source, int width, int height)
    {
        try
        {
            SKImageInfo resizeInfo = new SKImageInfo(width, height);

            SKSamplingOptions samplingOptions = new SKSamplingOptions(
                SKFilterMode.Linear,
                SKMipmapMode.None
            );

            return source.Resize(resizeInfo, samplingOptions);
        }
        catch
        {
            return null;
        }
    }

    private static int ToGray(SKColor color)
    {
        return (int)(
            0.299 * color.Red +
            0.587 * color.Green +
            0.114 * color.Blue
        );
    }

    private static bool IsValidImageFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (!File.Exists(path))
                return false;

            FileInfo fileInfo = new FileInfo(path);

            if (fileInfo.Length <= 100)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}