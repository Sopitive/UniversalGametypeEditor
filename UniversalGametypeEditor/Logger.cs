using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace UniversalGametypeEditor
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;

        static Logger()
        {
            // Create logs directory in application's folder
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            // Create log file with timestamp in name
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logsDirectory, $"GametypeEditor_{timestamp}.log");

            // Log application start
            LogInfo("Application started");
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);

            if (ex != null)
            {
                sb.Append($" | Exception: {ex.GetType().Name}");
                sb.Append($" | Message: {ex.Message}");
                sb.Append($" | StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    sb.Append($" | Inner Exception: {ex.InnerException.GetType().Name}");
                    sb.Append($" | Inner Message: {ex.InnerException.Message}");
                }
            }

            Log("ERROR", sb.ToString());
        }

        private static void Log(string level, string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            // Thread-safe writing to the log file
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }

            // Also output to debug console for development
            Debug.WriteLine(logEntry);
        }
    }
}
