# TTS Subtitle Converter

A C# WPF application that converts SRT subtitle files to speech using Edge-TTS API.

## Features

- Load and parse SRT subtitle files
- Support for multiple Edge-TTS voices (Japanese, Vietnamese, English, Korean, Chinese)
- Automatic TTS server startup via Docker
- Convert subtitles to speech with timeline synchronization
- Merge audio into a single file or save individual audio files
- Progress tracking and status updates
- Support for MP3 and WAV output formats

## Requirements

### System Requirements
- Windows 10/11 (WPF application)
- .NET 6.0 or later
- Visual Studio 2022 or .NET CLI

### Dependencies
- Docker Desktop (for automatic TTS server startup) OR
- Local Edge-TTS server executable

### NuGet Packages
- NAudio (2.2.1) - Audio processing and merging
- Newtonsoft.Json (13.0.3) - JSON serialization for API calls

## Building the Application

### Option 1: Visual Studio 2022
1. Open the `TTS.csproj` file in Visual Studio 2022
2. Restore NuGet packages (Build → Restore NuGet Packages)
3. Build the solution (Build → Build Solution)
4. Run the application (Debug → Start Debugging or F5)

### Option 2: .NET CLI
1. Open Command Prompt or PowerShell in the project directory
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the application:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

## TTS Server Setup

The application will automatically try to start the TTS server on startup. You have two options:

### Option A: Docker (Recommended)
1. Install Docker Desktop for Windows
2. The application will automatically run:
   ```bash
   docker run -d -p 5050:5050 travisvn/openai-edge-tts:latest
   ```

### Option B: Local Executable
1. Place the Edge-TTS server executable in one of these locations:
   - Same directory as the application: `edge-tts-server.exe`
   - In a `bin` subdirectory: `bin/edge-tts-server.exe`
2. The application will automatically detect and start it

## Usage

1. **Start the Application**: Launch the TTS Subtitle Converter
2. **Check Server Status**: The application will automatically check if the TTS server is running and try to start it
3. **Load SRT File**: Click "Open SRT File" and select your subtitle file
4. **Select Voice**: Choose from available Edge-TTS voices in the dropdown
5. **Convert to Speech**: Click "Convert to Speech" to process all subtitles
6. **Save Output**: 
   - Check "Merge into single file" for timeline-aligned audio
   - Uncheck to save individual audio files per subtitle
   - Choose MP3 or WAV format
   - Click "Save Output" to choose destination

## Supported Voices

- **Japanese**: ja-JP-NanamiNeural, ja-JP-KeitaNeural
- **Vietnamese**: vi-VN-HoaiMyNeural
- **English**: en-US-JennyNeural, en-US-DavisNeural
- **Korean**: ko-KR-SunHiNeural
- **Chinese**: zh-CN-XiaoxiaoNeural

## API Format

The application sends POST requests to `http://localhost:5050/v1/audio/speech` with:

```json
{
  "model": "gpt-4o-mini-tts",
  "voice": "<selected-voice>",
  "input": "<subtitle-text>"
}
```

## Project Structure

```
TTS/
├── TTS.csproj              # Project file
├── App.xaml                # Application definition
├── App.xaml.cs             # Application startup
├── MainWindow.xaml         # Main UI layout
├── MainWindow.xaml.cs      # UI logic and event handlers
├── Models/
│   └── SubtitleEntry.cs    # Subtitle data model
├── Services/
│   ├── SrtParser.cs        # SRT file parser
│   ├── TtsService.cs       # TTS API client
│   ├── ServerManager.cs    # Server management
│   └── AudioService.cs     # Audio merging and processing
└── README.md               # This file
```

## Troubleshooting

### Server Won't Start
- Ensure Docker Desktop is installed and running
- Check if port 5050 is available
- Try manually starting the server: `docker run -p 5050:5050 travisvn/openai-edge-tts:latest`

### Build Errors
- Ensure you're using .NET 6.0 or later
- Restore NuGet packages: `dotnet restore`
- Check that all dependencies are properly referenced

### Audio Issues
- Ensure output directory has write permissions
- Check that the selected output format is supported
- For MP3 output, the application may output WAV files (additional codec setup required for MP3)

## License

This project is provided as-is for educational and personal use.
