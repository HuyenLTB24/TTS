using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TTS.Services
{
    public class ServerManager
    {
        private Process? _serverProcess;

        public async Task<bool> EnsureServerRunningAsync()
        {
            var ttsService = new TtsService();
            
            // Check if server is already running
            if (await ttsService.IsServerRunningAsync())
            {
                return true;
            }

            // Try to start the server
            return await StartServerAsync();
        }

        private async Task<bool> StartServerAsync()
        {
            try
            {
                // First try to start with Docker
                if (await StartDockerServerAsync())
                {
                    return true;
                }

                // If Docker fails, try to start local executable (if it exists)
                if (await StartLocalExecutableAsync())
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start TTS server: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StartDockerServerAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "run -d -p 5050:5050 travisvn/openai-edge-tts:latest",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _serverProcess = Process.Start(startInfo);
                if (_serverProcess != null)
                {
                    await _serverProcess.WaitForExitAsync();
                    
                    // Wait a bit for the server to start up
                    await Task.Delay(5000);
                    
                    // Check if server is running
                    var ttsService = new TtsService();
                    return await ttsService.IsServerRunningAsync();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> StartLocalExecutableAsync()
        {
            try
            {
                // Check for common locations of a local TTS executable
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edge-tts-server.exe"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "edge-tts-server.exe"),
                    "edge-tts-server.exe"
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        _serverProcess = Process.Start(startInfo);
                        if (_serverProcess != null)
                        {
                            // Wait a bit for the server to start up
                            await Task.Delay(3000);
                            
                            // Check if server is running
                            var ttsService = new TtsService();
                            return await ttsService.IsServerRunningAsync();
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void StopServer()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }
            }
            catch
            {
                // Ignore errors when stopping
            }
        }

        public void Dispose()
        {
            StopServer();
        }
    }
}