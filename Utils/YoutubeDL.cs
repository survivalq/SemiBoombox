using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace SemiBoombox.Utils
{
    public static class YoutubeDL
    {
        private static readonly string baseFolder = Path.Combine(Directory.GetCurrentDirectory(), "SemiBoombox");
        private const string YTDLP_URL = "https://github.com/yt-dlp/yt-dlp/releases/download/2025.03.21/yt-dlp.exe";
        private const string YTDLP_VERSION = "2025.03.21";
        private static readonly string ytDlpPath = Path.Combine(baseFolder, "yt-dlp.exe");

        public static async Task InitializeAsync()
        {
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }

            if (File.Exists(ytDlpPath))
            {
                try
                {
                    string fileVersion = await GetYtDlpVersionAsync();

                    if (fileVersion != YTDLP_VERSION)
                    {
                        Console.WriteLine($"Updating yt-dlp from version {fileVersion} to {YTDLP_VERSION}.");
                        File.Delete(ytDlpPath);
                        await DownloadFileAsync(YTDLP_URL, ytDlpPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking yt-dlp version: {ex.Message}");
                    File.Delete(ytDlpPath);
                    await DownloadFileAsync(YTDLP_URL, ytDlpPath);
                }
            }
            else
            {
                Console.WriteLine("yt-dlp not found. Downloading...");
                await DownloadFileAsync(YTDLP_URL, ytDlpPath);
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

        private static async Task<string> GetYtDlpVersionAsync()
        {
            ProcessStartInfo versionInfo = new()
            {
                FileName = ytDlpPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(versionInfo) ?? throw new Exception("Failed to start yt-dlp process for version check.");
            string versionOutput = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();
            return versionOutput.Trim();
        }
    }
}