using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace SemiBoombox.Utils
{
    public static class YoutubeDL
    {
        private static readonly string baseFolder = Path.Combine(Directory.GetCurrentDirectory(), "SemiBoombox");
        private const string YtDLP_URL = "https://github.com/yt-dlp/yt-dlp/releases/download/2025.02.19/yt-dlp.exe";
        private static readonly string ytDlpPath = Path.Combine(baseFolder, "yt-dlp.exe");
        private static readonly string ffmpegFolder = Path.Combine(baseFolder, "ffmpeg");

        public static async Task InitializeAsync()
        {
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }

            if (Directory.Exists(ffmpegFolder))
            {
                Console.WriteLine("Removing deprecated ffmpeg dependency...");
                Directory.Delete(ffmpegFolder, true);
            }

            if (!File.Exists(ytDlpPath))
            {
                Console.WriteLine("yt-dlp not found. Downloading...");
                await DownloadFileAsync(YtDLP_URL, ytDlpPath);
            }

            Console.WriteLine("Initialization complete.");
        }

        private static async Task DownloadFileAsync(string url, string destinationPath)
        {
            using HttpClient client = new();
            byte[] data = await client.GetByteArrayAsync(url);
            File.WriteAllBytes(destinationPath, data);
        }

        public static async Task<string> DownloadAudioAsync(string videoUrl)
        {
            await InitializeAsync(); 

            string tempFolder = Path.Combine(baseFolder, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            Console.WriteLine("Downloading audio...");

            return await Task.Run(() =>
            {
                try
                {
                    string command = $"-f \"bestaudio[ext=m4a]\" -o \"{Path.Combine(tempFolder, "%(title)s.%(ext)s")}\" {videoUrl}";

                    ProcessStartInfo processInfo = new()
                    {
                        FileName = ytDlpPath,
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(processInfo))
                    {
                        if (process == null)
                        {
                            throw new Exception("Failed to start yt-dlp process.");
                        }

                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            string error = process.StandardError.ReadToEnd();
                            throw new Exception($"yt-dlp error: {error}");
                        }
                    }

                    string audioFilePath = Directory.GetFiles(tempFolder, "*.m4a").FirstOrDefault();
                    if (audioFilePath == null)
                    {
                        throw new Exception("Audio download failed.");
                    }

                    return audioFilePath;
                }
                catch (Exception ex)
                {
                    Directory.Delete(tempFolder, true);
                    throw new Exception($"Error downloading audio: {ex.Message}");
                }
            });
        }
    }
}