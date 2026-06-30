using System;
using System.IO;

namespace WebDog.Services
{
    public static class Logger
    {
        private static readonly string _filePath;
        private static readonly object _lock = new();

        static Logger()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            _filePath = Path.Combine(dir, "webdog.log");
            try { File.WriteAllText(_filePath, $"=== WebDog Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n"); } catch { }
        }

        public static void Info(string msg) => Write("INFO", msg, null);
        public static void Warn(string msg) => Write("WARN", msg, null);
        public static void Error(string msg, Exception ex = null) => Write("ERROR", msg, ex);

        private static void Write(string level, string msg, Exception ex)
        {
            try
            {
                lock (_lock)
                {
                    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {msg}";
                    if (ex != null) line += $" | {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                    File.AppendAllText(_filePath, line + "\n");
                }
            }
            catch { }
        }
    }
}
