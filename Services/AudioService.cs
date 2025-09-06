using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NAudio.MediaFoundation;
using TTS.Models;

namespace TTS.Services
{
    public class AudioService
    {
        public void MergeAudioFiles(List<SubtitleEntry> subtitles, Dictionary<int, byte[]> audioData, string outputPath)
        {
            var tempFiles = new List<string>();
            
            try
            {
                // Create temporary WAV files from MP3 data
                var audioSegments = new List<(TimeSpan start, TimeSpan end, string tempFile)>();
                
                foreach (var subtitle in subtitles.OrderBy(s => s.StartTime))
                {
                    if (audioData.ContainsKey(subtitle.Index))
                    {
                        var tempMp3File = Path.GetTempFileName() + ".mp3";
                        var tempWavFile = Path.GetTempFileName() + ".wav";
                        tempFiles.Add(tempMp3File);
                        tempFiles.Add(tempWavFile);
                        
                        // Write MP3 data to temporary file
                        File.WriteAllBytes(tempMp3File, audioData[subtitle.Index]);
                        
                        // Convert MP3 to WAV for processing
                        using (var mp3Reader = new Mp3FileReader(tempMp3File))
                        {
                            WaveFileWriter.CreateWaveFile(tempWavFile, mp3Reader);
                        }
                        
                        audioSegments.Add((subtitle.StartTime, subtitle.EndTime, tempWavFile));
                    }
                }
                
                // Create the merged audio file
                if (audioSegments.Any())
                {
                    CreateMergedAudioFile(audioSegments, outputPath);
                }
            }
            finally
            {
                // Clean up temporary files
                foreach (var tempFile in tempFiles)
                {
                    try
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        private void CreateMergedAudioFile(List<(TimeSpan start, TimeSpan end, string tempFile)> audioSegments, string outputPath)
        {
            if (!audioSegments.Any()) return;

            var extension = Path.GetExtension(outputPath).ToLower();
            var tempOutputFile = Path.GetTempFileName() + ".wav";

            try
            {
                // First create a WAV file with all segments
                CreateWavWithSilenceGaps(audioSegments, tempOutputFile);
                
                // Then convert to final format if needed
                if (extension == ".mp3")
                {
                    ConvertWavToMp3(tempOutputFile, outputPath);
                }
                else
                {
                    File.Copy(tempOutputFile, outputPath, true);
                }
            }
            finally
            {
                if (File.Exists(tempOutputFile))
                {
                    File.Delete(tempOutputFile);
                }
            }
        }
        
        private void CreateWavWithSilenceGaps(List<(TimeSpan start, TimeSpan end, string tempFile)> audioSegments, string outputPath)
        {
            if (!audioSegments.Any()) return;

            var firstSegment = audioSegments.First();
            using var firstReader = new WaveFileReader(firstSegment.tempFile);
            var format = firstReader.WaveFormat;
            
            var totalDuration = audioSegments.Max(s => s.end);
            
            using var outputWriter = new WaveFileWriter(outputPath, format);
            
            var currentTime = TimeSpan.Zero;
            
            foreach (var (start, end, tempFile) in audioSegments.OrderBy(s => s.start))
            {
                // Add silence if there's a gap
                if (start > currentTime)
                {
                    var silenceDuration = start - currentTime;
                    WriteSilence(outputWriter, format, silenceDuration);
                }
                
                // Add the audio segment
                using (var reader = new WaveFileReader(tempFile))
                {
                    reader.CopyTo(outputWriter);
                }
                
                currentTime = start + GetAudioDuration(tempFile);
            }
        }
        
        private void WriteSilence(WaveFileWriter writer, WaveFormat format, TimeSpan duration)
        {
            var samplesNeeded = (int)(duration.TotalSeconds * format.SampleRate * format.Channels);
            var silenceBuffer = new byte[samplesNeeded * (format.BitsPerSample / 8)];
            writer.Write(silenceBuffer, 0, silenceBuffer.Length);
        }
        
        private TimeSpan GetAudioDuration(string audioFile)
        {
            using var reader = new WaveFileReader(audioFile);
            return reader.TotalTime;
        }
        
        private void ConvertWavToMp3(string wavFile, string mp3File)
        {
            // For now, just copy the WAV file - proper MP3 encoding would require additional packages
            // In a real implementation, you would use NAudio.Lame or MediaFoundationEncoder
            File.Copy(wavFile, mp3File.Replace(".mp3", ".wav"), true);
        }
        
        public void SaveIndividualAudioFiles(List<SubtitleEntry> subtitles, Dictionary<int, byte[]> audioData, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            foreach (var subtitle in subtitles)
            {
                if (audioData.ContainsKey(subtitle.Index))
                {
                    var fileName = $"subtitle_{subtitle.Index:D3}.mp3";
                    var filePath = Path.Combine(outputDirectory, fileName);
                    File.WriteAllBytes(filePath, audioData[subtitle.Index]);
                }
            }
        }
    }
}