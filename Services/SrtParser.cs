using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TTS.Models;

namespace TTS.Services
{
    public class SrtParser
    {
        public static List<SubtitleEntry> ParseSrtFile(string filePath)
        {
            var subtitles = new List<SubtitleEntry>();
            var content = File.ReadAllText(filePath);
            
            // Split content by double newlines to separate subtitle blocks
            var blocks = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var block in blocks)
            {
                var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length < 3) continue;
                
                // First line should be the index
                if (!int.TryParse(lines[0].Trim(), out int index)) continue;
                
                // Second line should be the timestamp
                var timeMatch = Regex.Match(lines[1], @"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})");
                if (!timeMatch.Success) continue;
                
                var startTime = new TimeSpan(0, 
                    int.Parse(timeMatch.Groups[1].Value), 
                    int.Parse(timeMatch.Groups[2].Value), 
                    int.Parse(timeMatch.Groups[3].Value), 
                    int.Parse(timeMatch.Groups[4].Value));
                    
                var endTime = new TimeSpan(0, 
                    int.Parse(timeMatch.Groups[5].Value), 
                    int.Parse(timeMatch.Groups[6].Value), 
                    int.Parse(timeMatch.Groups[7].Value), 
                    int.Parse(timeMatch.Groups[8].Value));
                
                // Remaining lines are the subtitle text
                var text = string.Join(" ", lines, 2, lines.Length - 2);
                
                subtitles.Add(new SubtitleEntry
                {
                    Index = index,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = text.Trim()
                });
            }
            
            return subtitles;
        }
    }
}