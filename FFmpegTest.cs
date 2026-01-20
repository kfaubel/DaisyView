using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

class FFmpegTest
{
    static void Main()
    {
        string webmPath = Path.Combine(Path.GetTempPath(), "test_video.webm");
        string mp4Path = Path.Combine(Path.GetTempPath(), "test_output.mp4");

        Console.WriteLine("FFmpeg Conversion Test");
        Console.WriteLine("=====================");
        Console.WriteLine($"WebM input:  {webmPath}");
        Console.WriteLine($"MP4 output:  {mp4Path}");
        Console.WriteLine();

        // Create test WebM if needed
        if (!File.Exists(webmPath))
        {
            Console.WriteLine("Creating test WebM file...");
            var createPsi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-f lavfi -i color=black:s=320x240:d=1 -f lavfi -i sine=f=1000:d=1 -c:v libvpx-vp9 -c:a libopus \"{webmPath}\" -y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(createPsi))
            {
                p?.StandardOutput.ReadToEnd();
                p?.StandardError.ReadToEnd();
                p?.WaitForExit();
            }
            Console.WriteLine("Test WebM created");
        }

        // Clean up old output
        if (File.Exists(mp4Path)) File.Delete(mp4Path);

        Console.WriteLine();
        Console.WriteLine("Starting conversion...");

        // Test the exact pattern from VideoConversionService
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -i \"{webmPath}\" -c:v libx264 -preset fast -c:a aac -q:a 5 \"{mp4Path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using (var process = Process.Start(psi))
        {
            if (process == null)
            {
                Console.WriteLine("ERROR: Failed to start FFmpeg");
                return;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            System.Threading.Tasks.Task.WaitAll(stdoutTask, stderrTask);

            Console.WriteLine($"FFmpeg exit code: {process.ExitCode}");

            if (process.ExitCode != 0)
            {
                Console.WriteLine();
                Console.WriteLine("FFmpeg stderr output:");
                Console.WriteLine(stderrTask.Result);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Output file exists: {File.Exists(mp4Path)}");
        if (File.Exists(mp4Path))
        {
            var fileSize = new FileInfo(mp4Path).Length;
            Console.WriteLine($"Output file size: {fileSize} bytes");
            Console.WriteLine();
            Console.WriteLine("SUCCESS: Conversion completed!");
        }
        else
        {
            Console.WriteLine("FAILED: No output file created");
        }
    }
}
