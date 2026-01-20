using System;
using System.Diagnostics;
using System.IO;

// Quick test to verify FFmpeg conversion works from C#
// Compile and run: csc TestFFmpegConversion.cs && TestFFmpegConversion.exe

class Program
{
    static void Main()
    {
        // Use the test WebM we created
        string webmPath = $@"{Path.GetTempPath()}test_video.webm";
        string mp4Path = $@"{Path.GetTempPath()}test_csharp_output.mp4";

        if (!File.Exists(webmPath))
        {
            Console.WriteLine($"ERROR: Test WebM not found at {webmPath}");
            return;
        }

        Console.WriteLine($"Testing conversion:");
        Console.WriteLine($"Input:  {webmPath}");
        Console.WriteLine($"Output: {mp4Path}");
        Console.WriteLine();

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{webmPath}"" -c:v libx264 -preset fast -c:a aac -q:a 5 ""{mp4Path}"" -y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            Console.WriteLine($"Command: ffmpeg {processInfo.Arguments}");
            Console.WriteLine();

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("ERROR: Failed to start FFmpeg process!");
                    return;
                }

                // Read output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Console.WriteLine($"Exit Code: {process.ExitCode}");
                Console.WriteLine();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("ERROR Output (stderr):");
                    Console.WriteLine(error);
                }
                else
                {
                    Console.WriteLine("SUCCESS!");
                }

                Console.WriteLine();
                Console.WriteLine($"Output file exists: {File.Exists(mp4Path)}");
                if (File.Exists(mp4Path))
                {
                    Console.WriteLine($"Output file size: {new FileInfo(mp4Path).Length} bytes");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
