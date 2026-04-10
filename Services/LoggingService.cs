using System;
using System.Text;

namespace LocalAIAgent.Services
{
    public class LoggingService
    {
        private readonly StringBuilder _buffer = new();

        public event Action<string>? LogUpdated;

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        private void Write(string level, string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            _buffer.AppendLine(line);
            LogUpdated?.Invoke(_buffer.ToString());
        }

        public string GetLog() => _buffer.ToString();
    }
}
