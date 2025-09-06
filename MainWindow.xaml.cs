using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TTS.Models;
using TTS.Services;

namespace TTS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<SubtitleEntry> _subtitles = new();
        private readonly TtsService _ttsService = new();
        private readonly ServerManager _serverManager = new();
        private readonly AudioService _audioService = new();
        private readonly Dictionary<int, byte[]> _audioData = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
            DgSubtitles.ItemsSource = _subtitles;
            
            // Check server status on startup
            _ = CheckServerStatusAsync();
        }

        private async void BtnOpenSrt_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SRT Files (*.srt)|*.srt|All Files (*.*)|*.*",
                Title = "Select SRT Subtitle File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var subtitles = SrtParser.ParseSrtFile(openFileDialog.FileName);
                    
                    _subtitles.Clear();
                    foreach (var subtitle in subtitles)
                    {
                        _subtitles.Add(subtitle);
                    }
                    
                    TxtFileName.Text = Path.GetFileName(openFileDialog.FileName);
                    TxtStatusBar.Text = $"Loaded {subtitles.Count} subtitle entries";
                    
                    // Clear previous audio data
                    _audioData.Clear();
                    BtnSaveOutput.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading SRT file: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnCheckServer_Click(object sender, RoutedEventArgs e)
        {
            await CheckServerStatusAsync();
        }

        private async Task CheckServerStatusAsync()
        {
            try
            {
                TxtServerStatus.Text = "Checking...";
                var isRunning = await _ttsService.IsServerRunningAsync();
                
                if (isRunning)
                {
                    TxtServerStatus.Text = "Running";
                    TxtServerStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    TxtServerStatus.Text = "Not Running";
                    TxtServerStatus.Foreground = System.Windows.Media.Brushes.Red;
                    
                    // Try to start the server
                    TxtStatusBar.Text = "Starting TTS server...";
                    var started = await _serverManager.EnsureServerRunningAsync();
                    
                    if (started)
                    {
                        TxtServerStatus.Text = "Running";
                        TxtServerStatus.Foreground = System.Windows.Media.Brushes.Green;
                        TxtStatusBar.Text = "TTS server started successfully";
                    }
                    else
                    {
                        TxtStatusBar.Text = "Failed to start TTS server. Please ensure Docker is installed and running.";
                        MessageBox.Show("Failed to start TTS server. Please ensure Docker is installed and running, or manually start the server at localhost:5050", 
                            "Server Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                TxtServerStatus.Text = "Error";
                TxtServerStatus.Foreground = System.Windows.Media.Brushes.Red;
                TxtStatusBar.Text = $"Error checking server: {ex.Message}";
            }
        }

        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (!_subtitles.Any())
            {
                MessageBox.Show("Please load an SRT file first.", "No Subtitles", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbVoices.SelectedItem is not ComboBoxItem selectedVoice)
            {
                MessageBox.Show("Please select a voice.", "No Voice Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if server is running
            if (!await _ttsService.IsServerRunningAsync())
            {
                MessageBox.Show("TTS server is not running. Please check the server status.", "Server Not Running", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await StartConversionAsync(selectedVoice.Content.ToString()!);
        }

        private async Task StartConversionAsync(string voice)
        {
            _isProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            BtnConvert.IsEnabled = false;
            BtnStop.IsEnabled = true;
            BtnSaveOutput.IsEnabled = false;
            
            ProgressMain.Maximum = _subtitles.Count;
            ProgressMain.Value = 0;
            
            _audioData.Clear();
            
            try
            {
                var processedCount = 0;
                var totalCount = _subtitles.Count;
                
                foreach (var subtitle in _subtitles)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;
                    
                    subtitle.Status = "Processing...";
                    TxtProgress.Text = $"Processing subtitle {processedCount + 1} of {totalCount}";
                    
                    try
                    {
                        var audioData = await _ttsService.GenerateSpeechAsync(subtitle.Text, voice);
                        _audioData[subtitle.Index] = audioData;
                        subtitle.Status = "Completed";
                    }
                    catch (Exception ex)
                    {
                        subtitle.Status = $"Error: {ex.Message}";
                        TxtStatusBar.Text = $"Error processing subtitle {subtitle.Index}: {ex.Message}";
                    }
                    
                    processedCount++;
                    ProgressMain.Value = processedCount;
                    
                    // Allow UI to update
                    await Task.Delay(10);
                }
                
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    TxtProgress.Text = "Conversion cancelled";
                    TxtStatusBar.Text = "Conversion was cancelled by user";
                }
                else
                {
                    TxtProgress.Text = $"Conversion completed: {_audioData.Count} of {totalCount} subtitles processed";
                    TxtStatusBar.Text = "Conversion completed successfully";
                    BtnSaveOutput.IsEnabled = _audioData.Count > 0;
                }
            }
            catch (Exception ex)
            {
                TxtProgress.Text = "Conversion failed";
                TxtStatusBar.Text = $"Conversion failed: {ex.Message}";
                MessageBox.Show($"Conversion failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                BtnConvert.IsEnabled = true;
                BtnStop.IsEnabled = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private async void BtnSaveOutput_Click(object sender, RoutedEventArgs e)
        {
            if (!_audioData.Any())
            {
                MessageBox.Show("No audio data to save. Please convert subtitles first.", "No Audio Data", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var format = (CmbOutputFormat.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "mp3";
            
            if (ChkMergeAudio.IsChecked == true)
            {
                await SaveMergedAudioAsync(format);
            }
            else
            {
                await SaveIndividualAudioFilesAsync();
            }
        }

        private async Task SaveMergedAudioAsync(string format)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = format == "mp3" ? "MP3 Files (*.mp3)|*.mp3" : "WAV Files (*.wav)|*.wav",
                Title = "Save Merged Audio File",
                FileName = $"merged_subtitles.{format}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    TxtStatusBar.Text = "Merging audio files...";
                    TxtProgress.Text = "Merging audio with timeline alignment...";
                    
                    await Task.Run(() =>
                    {
                        _audioService.MergeAudioFiles(_subtitles.ToList(), _audioData, saveFileDialog.FileName);
                    });
                    
                    TxtOutputPath.Text = saveFileDialog.FileName;
                    TxtStatusBar.Text = "Audio file saved successfully";
                    TxtProgress.Text = "Merge completed";
                    
                    MessageBox.Show($"Audio file saved successfully to:\n{saveFileDialog.FileName}", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    TxtStatusBar.Text = $"Error saving audio: {ex.Message}";
                    MessageBox.Show($"Error saving audio file: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task SaveIndividualAudioFilesAsync()
        {
            // Use FolderBrowserDialog alternative approach
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Select Output Folder (file will be ignored, folder will be used)",
                FileName = "select_folder",
                Filter = "Folder Selection|*.folder"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var outputFolder = Path.GetDirectoryName(saveFileDialog.FileName);
                    if (string.IsNullOrEmpty(outputFolder))
                    {
                        MessageBox.Show("Invalid folder selection.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Create a subfolder for the audio files
                    var audioFolder = Path.Combine(outputFolder, "TTS_Audio_Files");
                    
                    TxtStatusBar.Text = "Saving individual audio files...";
                    TxtProgress.Text = "Saving individual audio files...";
                    
                    await Task.Run(() =>
                    {
                        _audioService.SaveIndividualAudioFiles(_subtitles.ToList(), _audioData, audioFolder);
                    });
                    
                    TxtOutputPath.Text = audioFolder;
                    TxtStatusBar.Text = "Individual audio files saved successfully";
                    TxtProgress.Text = "Individual files saved";
                    
                    MessageBox.Show($"Individual audio files saved successfully to:\n{audioFolder}", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    TxtStatusBar.Text = $"Error saving audio files: {ex.Message}";
                    MessageBox.Show($"Error saving audio files: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _ttsService?.Dispose();
            _serverManager?.Dispose();
            base.OnClosed(e);
        }
    }
}