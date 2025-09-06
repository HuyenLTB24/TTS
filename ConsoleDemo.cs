using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TTS.Models;
using TTS.Services;

namespace TTS
{
    /// <summary>
    /// Console demo of the TTS functionality (for testing on non-Windows environments)
    /// </summary>
    public class ConsoleDemo
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("TTS Subtitle Converter - Console Demo");
            Console.WriteLine("=====================================");
            
            // Check if sample SRT file exists
            var srtFile = "sample.srt";
            if (!File.Exists(srtFile))
            {
                Console.WriteLine($"Sample SRT file '{srtFile}' not found.");
                return;
            }
            
            try
            {
                // Parse SRT file
                Console.WriteLine($"Parsing SRT file: {srtFile}");
                var subtitles = SrtParser.ParseSrtFile(srtFile);
                Console.WriteLine($"Found {subtitles.Count} subtitle entries:");
                
                foreach (var subtitle in subtitles)
                {
                    Console.WriteLine($"  {subtitle.Index}: {subtitle.TimeRange} - {subtitle.Text}");
                }
                
                // Test TTS service (will likely fail without server)
                Console.WriteLine("\nTesting TTS service connection...");
                var ttsService = new TtsService();
                var isServerRunning = await ttsService.IsServerRunningAsync();
                
                if (isServerRunning)
                {
                    Console.WriteLine("✓ TTS server is running at localhost:5050");
                    
                    // Demo: Convert first subtitle
                    if (subtitles.Count > 0)
                    {
                        Console.WriteLine($"\nConverting first subtitle to speech...");
                        var firstSubtitle = subtitles[0];
                        
                        try
                        {
                            var audioData = await ttsService.GenerateSpeechAsync(firstSubtitle.Text, "ja-JP-NanamiNeural");
                            Console.WriteLine($"✓ Generated {audioData.Length} bytes of audio data for: {firstSubtitle.Text}");
                            
                            // Save audio file
                            var outputFile = "output_demo.mp3";
                            File.WriteAllBytes(outputFile, audioData);
                            Console.WriteLine($"✓ Saved audio to: {outputFile}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Error generating speech: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("✗ TTS server is not running at localhost:5050");
                    Console.WriteLine("  To run the server manually:");
                    Console.WriteLine("  docker run -d -p 5050:5050 travisvn/openai-edge-tts:latest");
                }
                
                Console.WriteLine("\nDemo completed. On Windows, run the WPF application for the full GUI experience.");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}