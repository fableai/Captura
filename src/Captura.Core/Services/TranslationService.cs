using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace Captura.Services
{
    public class TranslationService : ITranslationService
    {
        private Process _pythonProcess;
        private bool _disposed;
        private readonly string _pythonScriptPath;
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public TranslationService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Captura"
            );
            Directory.CreateDirectory(appDataPath);
            _pythonScriptPath = Path.Combine(appDataPath, "main.py");

            if (!File.Exists(_pythonScriptPath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "main.py"), _pythonScriptPath);
            }
        }

        public async Task Start(string apiKey)
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                return;
            }

            _cts = new CancellationTokenSource();

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{_pythonScriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["ZHIPU_API_KEY"] = apiKey;

            try
            {
                _pythonProcess = Process.Start(startInfo);

                await Task.Delay(2000);

                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri("ws://localhost:8765"), _cts.Token);
                _isConnected = true;

                var config = new { type = "config", api_key = apiKey };
                var configJson = JsonConvert.SerializeObject(config);
                var configBytes = Encoding.UTF8.GetBytes(configJson);
                await _webSocket.SendAsync(new ArraySegment<byte>(configBytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new TranslationServiceException("Failed to start translation service", ex);
            }
        }

        public async Task Stop()
        {
            _isConnected = false;

            if (_webSocket != null)
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping service", CancellationToken.None);
                    _webSocket.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing WebSocket: {ex.Message}");
                }
            }

            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                try
                {
                    _pythonProcess.Kill();
                    await _pythonProcess.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to stop translation service: {ex.Message}");
                }
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public async Task StreamAudioData(byte[] buffer, int bytesRecorded)
        {
            if (!_isConnected || _webSocket == null) return;

            try
            {
                var segment = new ArraySegment<byte>(buffer, 0, bytesRecorded);
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, _cts.Token);
            }
            catch (Exception)
            {
                _isConnected = false;
                await TryReconnect();
            }
        }

        private async Task TryReconnect()
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await Start(_pythonProcess.StartInfo.EnvironmentVariables["ZHIPU_API_KEY"]);
                    return;
                }
                catch
                {
                    await Task.Delay(1000 * (i + 1));
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop().Wait();
                _pythonProcess?.Dispose();
                _webSocket?.Dispose();
                _cts?.Dispose();
                _disposed = true;
            }
        }
    }

    public class TranslationServiceException : Exception
    {
        public TranslationServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
