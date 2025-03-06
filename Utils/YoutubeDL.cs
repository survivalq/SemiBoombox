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
        private const string FFMPEG_URL = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        private static readonly string ytDlpPath = Path.Combine(baseFolder, "yt-dlp.exe");
        private static readonly string ffmpegFolder = Path.Combine(baseFolder, "ffmpeg");
        private static readonly string ffmpegBinPath = Path.Combine(ffmpegFolder, "ffmpeg-master-latest-win64-gpl", "bin", "ffmpeg.exe");

        public static async Task InitializeAsync()
        {
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }

            if (!File.Exists(ytDlpPath))
            {
                Console.WriteLine("yt-dlp not found. Downloading...");
                await DownloadFileAsync(YtDLP_URL, ytDlpPath);
            }

            if (!Directory.Exists(ffmpegFolder))
            {
                Console.WriteLine("ffmpeg not found. Downloading and extracting...");
                await DownloadAndExtractFFmpegAsync();
            }

            if (!File.Exists(ffmpegBinPath))
            {
                throw new Exception("ffmpeg executable was not found after extraction.");
            }

            Console.WriteLine("Initialization complete.");
        }

        private static async Task DownloadFileAsync(string url, string destinationPath)
        {
            using HttpClient client = new();
            byte[] data = await client.GetByteArrayAsync(url);
            File.WriteAllBytes(destinationPath, data);
        }

        private static async Task DownloadAndExtractFFmpegAsync()
        {
            string zipPath = Path.Combine(baseFolder, "ffmpeg.zip");

            await DownloadFileAsync(FFMPEG_URL, zipPath);

            ZipFile.ExtractToDirectory(zipPath, ffmpegFolder);
            File.Delete(zipPath);
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
                    string command = $"-x --audio-format mp3 --ffmpeg-location \"{ffmpegBinPath}\" --output \"{Path.Combine(tempFolder, "%(title)s.%(ext)s")}\" {videoUrl}";

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

                    string audioFilePath = Directory.GetFiles(tempFolder, "*.mp3").FirstOrDefault();
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