namespace Itqueuein_Check.Logging
{
    internal class FileLogger
    {
        private readonly string _logDirectory;

        public FileLogger()
        {
            // Crea la carpeta Logs donde esté el ejecutable
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void LogInfo(string message)
        {
            WriteToFile("INFO", message);
        }

        public void LogError(string message, Exception ex = null)
        {
            string fullMessage = ex == null ? message : $"{message} | Ex: {ex.Message}";
            WriteToFile("ERROR", fullMessage);
        }

        private void WriteToFile(string level, string message)
        {
            try
            {
                string filePath = Path.Combine(_logDirectory, $"CheckerLog_{DateTime.Now:yyyyMMdd}.txt");
                string logLine = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

                File.AppendAllText(filePath, logLine);
            }
            catch
            {
                // Falla silenciosa: si el disco está lleno o el archivo bloqueado, 
                // no queremos que el checker se caiga.
            }
        }
    }
}
