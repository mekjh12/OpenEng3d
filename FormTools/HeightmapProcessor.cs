using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class HeightmapProcessor
{
    
    public static void ProcessHeightmap(string inputPath, string outputPath, int blurRadius, int noiseAmount)
    {
        using (Bitmap originalImage = new Bitmap(inputPath))
        {
            Console.WriteLine($"Processing image of size {originalImage.Width}x{originalImage.Height}...");

            // Step 1: Apply Box Blur (faster than Gaussian for large images)
            Console.WriteLine("Applying blur filter...");
            Bitmap blurredImage = ApplyBoxBlur(originalImage, blurRadius);

            // Step 2: Apply Noise
            //Console.WriteLine("Adding noise...");
            //Bitmap finalImage = AddNoiseParallel(blurredImage, noiseAmount);

            // Save the processed image
            blurredImage.Save(outputPath, ImageFormat.Png);

            // Cleanup
            if (blurredImage != originalImage)
                blurredImage.Dispose();
        }
    }

    public static Bitmap ApplyBoxBlur(Bitmap image, int radius)
    {
        // Box blur is much faster than Gaussian blur and works well enough for heightmaps
        int width = image.Width;
        int height = image.Height;

        // Direct Bitmap access for better performance
        Bitmap result = new Bitmap(width, height);

        // Lock bits for faster processing
        BitmapData sourceData = image.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        BitmapData resultData = result.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        int stride = sourceData.Stride;
        IntPtr sourcePtr = sourceData.Scan0;
        IntPtr resultPtr = resultData.Scan0;

        int pixelSize = 4; // ARGB
        int kernelSize = radius * 2 + 1;

        // Allocate memory for processing
        byte[] sourceBuffer = new byte[stride * height];
        byte[] resultBuffer = new byte[stride * height];

        // Copy source to buffer
        Marshal.Copy(sourcePtr, sourceBuffer, 0, sourceBuffer.Length);

        // Process horizontally in parallel
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                int[] sum = new int[4] { 0, 0, 0, 0 };
                int count = 0;

                // Box blur kernel
                for (int i = -radius; i <= radius; i++)
                {
                    int currentX = x + i;
                    if (currentX >= 0 && currentX < width)
                    {
                        int pos = y * stride + currentX * pixelSize;
                        for (int c = 0; c < 4; c++)
                        {
                            sum[c] += sourceBuffer[pos + c];
                        }
                        count++;
                    }
                }

                // Write result
                int resultPos = y * stride + x * pixelSize;
                for (int c = 0; c < 4; c++)
                {
                    resultBuffer[resultPos + c] = (byte)(sum[c] / count);
                }
            }
        });

        // Vertical pass
        byte[] tempBuffer = new byte[resultBuffer.Length];
        Array.Copy(resultBuffer, tempBuffer, resultBuffer.Length);

        // Process vertically in parallel
        Parallel.For(0, width, x =>
        {
            for (int y = 0; y < height; y++)
            {
                int[] sum = new int[4] { 0, 0, 0, 0 };
                int count = 0;

                // Box blur kernel
                for (int i = -radius; i <= radius; i++)
                {
                    int currentY = y + i;
                    if (currentY >= 0 && currentY < height)
                    {
                        int pos = currentY * stride + x * pixelSize;
                        for (int c = 0; c < 4; c++)
                        {
                            sum[c] += tempBuffer[pos + c];
                        }
                        count++;
                    }
                }

                // Write result
                int resultPos = y * stride + x * pixelSize;
                for (int c = 0; c < 4; c++)
                {
                    resultBuffer[resultPos + c] = (byte)(sum[c] / count);
                }
            }
        });

        // Copy result back
        Marshal.Copy(resultBuffer, 0, resultPtr, resultBuffer.Length);

        // Unlock bits
        image.UnlockBits(sourceData);
        result.UnlockBits(resultData);

        return result;
    }

    static Bitmap AddNoiseParallel(Bitmap image, int noiseAmount)
    {
        int width = image.Width;
        int height = image.Height;

        Bitmap result = new Bitmap(width, height);

        // Lock bits for faster processing
        BitmapData sourceData = image.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        BitmapData resultData = result.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        int stride = sourceData.Stride;
        IntPtr sourcePtr = sourceData.Scan0;
        IntPtr resultPtr = resultData.Scan0;

        int pixelSize = 4; // ARGB

        // Allocate memory for processing
        byte[] sourceBuffer = new byte[stride * height];
        byte[] resultBuffer = new byte[stride * height];

        // Copy source to buffer
        Marshal.Copy(sourcePtr, sourceBuffer, 0, sourceBuffer.Length);

        // Use multiple random generators for parallel processing
        Random[] randoms = new Random[Environment.ProcessorCount];
        for (int i = 0; i < randoms.Length; i++)
        {
            randoms[i] = new Random(i + Environment.TickCount);
        }

        // Process in parallel
        Parallel.For(0, height, y =>
        {
            // Get thread-specific random generator
            Random random = randoms[y % randoms.Length];

            for (int x = 0; x < width; x++)
            {
                int offset = y * stride + x * pixelSize;

                // For heightmaps, apply the same noise to all channels
                int noise = random.Next(-noiseAmount, noiseAmount + 1);

                for (int c = 0; c < 3; c++) // Apply to RGB channels
                {
                    int value = sourceBuffer[offset + c] + noise;
                    // Clamp to byte range
                    resultBuffer[offset + c] = (byte)Math.Max(0, Math.Min(255, value));
                }

                // Keep alpha channel unchanged
                resultBuffer[offset + 3] = sourceBuffer[offset + 3];
            }
        });

        // Copy result back
        Marshal.Copy(resultBuffer, 0, resultPtr, resultBuffer.Length);

        // Unlock bits
        image.UnlockBits(sourceData);
        result.UnlockBits(resultData);

        return result;
    }

    public static void CreateComparisonImage(string originalPath, string processedPath, string outputPath)
    {
        using (Bitmap original = new Bitmap(originalPath))
        using (Bitmap processed = new Bitmap(processedPath))
        {
            // Create a side-by-side comparison image
            int width = original.Width;
            int height = original.Height;

            using (Bitmap comparison = new Bitmap(width * 2, height))
            using (Graphics g = Graphics.FromImage(comparison))
            {
                // Draw original on left side
                g.DrawImage(original, 0, 0, width, height);

                // Draw processed on right side
                g.DrawImage(processed, width, 0, width, height);

                // Draw dividing line
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    g.DrawLine(pen, width, 0, width, height);
                }

                // Add labels
                using (Font font = new Font("Arial", 20, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.White))
                using (SolidBrush shadowBrush = new SolidBrush(Color.Black))
                {
                    // Add background rectangles for better readability
                    g.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), 10, 10, 150, 40);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), width + 10, 10, 150, 40);

                    // Draw shadow and then text for better visibility
                    g.DrawString("Original", font, shadowBrush, 12, 12);
                    g.DrawString("Original", font, brush, 10, 10);

                    g.DrawString("Processed", font, shadowBrush, width + 12, 12);
                    g.DrawString("Processed", font, brush, width + 10, 10);
                }

                // Save the comparison image
                comparison.Save(outputPath, ImageFormat.Png);
            }
        }
    }
}