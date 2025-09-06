Write a C# WPF application (for .NET 6 or later) with the following requirements:

1. GUI requirements:
   - A menu or button to open an SRT file.
   - A DataGrid to display subtitle entries (columns: Time, Text, Status).
   - A ComboBox to select a voice (examples: ja-JP-NanamiNeural, ja-JP-KeitaNeural, vi-VN-HoaiMyNeural, en-US-JennyNeural).
   - A Button "Convert to Speech".
   - A ProgressBar to show progress.
   - A Button "Save Output" to choose the output file (MP3 or WAV).

2. Functionality:
   - On startup, the application should check if the local Edge-TTS API server is running at http://localhost:5050/v1/audio/speech.
   - If not running, automatically start it using `Process.Start`:
       - Option A: Start a Docker container with command:
         docker run -d -p 5050:5050 travisvn/openai-edge-tts:latest
       - Option B: If a local executable is bundled, run it directly.
   - Parse the SRT file: extract text and timeline.
   - For each subtitle entry, send a POST request to the local TTS API:
       {
         "model": "gpt-4o-mini-tts",
         "voice": "<selected voice>",
         "input": "<subtitle text>"
       }
   - Receive MP3 audio data and save it.
   - Either save one audio per subtitle entry or merge all into a single audio file aligned with the SRT timeline (use NAudio for merging and silence padding).

3. Requirements:
   - Include MainWindow.xaml + MainWindow.xaml.cs.
   - Include an SRT parser in C#.
   - Use HttpClient for API calls.
   - Use NAudio for audio merging.
   - Show progress updates while generating speech.
   - Instructions for building with Visual Studio 2022 or dotnet CLI.

Goal: I want a standalone WPF app on Windows that:
- Automatically launches the local TTS server if not running.
- Lets the user load an SRT file (Japanese or other language).
- Converts it into speech using Edge-TTS voices.
- Saves the final synchronized audio as MP3/WAV.
