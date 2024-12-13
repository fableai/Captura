using System;
using System.Diagnostics;
using System.IO;

namespace Captura.Services
{
    public class TranslationService : IDisposable
    {
        private Process _pythonProcess;
        private bool _disposed;
        private readonly string _pythonScriptPath;

        public TranslationService()
        {
            // Store main.py in the application's data directory
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Captura"
            );
            Directory.CreateDirectory(appDataPath);
            _pythonScriptPath = Path.Combine(appDataPath, "main.py");

            // Copy main.py to the application data directory if it doesn't exist
            if (!File.Exists(_pythonScriptPath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "main.py"), _pythonScriptPath);
            }
        }

        public void Start(string apiKey)
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                return;
            }

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start translation service: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                try
                {
                    _pythonProcess.Kill();
                    _pythonProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to stop translation service: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _pythonProcess?.Dispose();
                _disposed = true;
            }
        }
    }
}
