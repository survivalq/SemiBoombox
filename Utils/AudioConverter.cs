using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using UnityEngine;

namespace SemiBoombox.Utils
{
    public static class AudioConverter
    {
        public static string ConvertM4AToWav(string inputFile)
        {
            string outputFile = Path.ChangeExtension(inputFile, ".wav");

            using (var reader = new MediaFoundationReader(inputFile))
            using (var writer = new WaveFileWriter(outputFile, reader.WaveFormat))
            {
                reader.CopyTo(writer);
            }

            return outputFile;
        }

        public static async Task<AudioClip> WavToAudioClipAsync(string filePath)
        {
            byte[] wavBytes;
            
            using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
            {
                wavBytes = new byte[fileStream.Length];
                await fileStream.ReadAsync(wavBytes, 0, wavBytes.Length);
            }

            int headerOffset = 44;
            short channels = BitConverter.ToInt16(wavBytes, 22);
            int sampleRate = BitConverter.ToInt32(wavBytes, 24);
            short bitsPerSample = BitConverter.ToInt16(wavBytes, 34);

            int dataLength = wavBytes.Length - headerOffset;
            int bytesPerSample = bitsPerSample / 8;
            int totalSamples = dataLength / bytesPerSample;

            float[] audioData = new float[totalSamples];

            if (bitsPerSample == 16)
            {
                for (int i = 0; i < totalSamples; i++)
                {
                    short sample = BitConverter.ToInt16(wavBytes, headerOffset + i * bytesPerSample);
                    audioData[i] = sample / 32768f;
                }
            }
            else
            {
                throw new NotSupportedException("Only 16-bit PCM WAV files are supported in this example.");
            }

            int sampleCount = totalSamples / channels;

            AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(filePath), sampleCount, channels, sampleRate, false);

            clip.SetData(audioData, 0);

            return clip;
        }

        public static async Task<AudioClip> GetAudioClipAsync(string filePath)
        {
            string wavFile = ConvertM4AToWav(filePath);

            AudioClip clip = await WavToAudioClipAsync(wavFile);

            string folderPath = Path.GetDirectoryName(filePath);
            if (File.Exists(filePath))
                File.Delete(filePath);

            if (File.Exists(wavFile))
                File.Delete(wavFile);

            if (Directory.Exists(folderPath) && Directory.GetFileSystemEntries(folderPath).Length == 0)
            {
                Directory.Delete(folderPath);
            }

            return clip;
        }
    }
}