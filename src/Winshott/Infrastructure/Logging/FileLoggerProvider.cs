using Microsoft.Extensions.Logging;

namespace Winshott.Infrastructure.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _gate = new();
    private bool _disposed;

    public FileLoggerProvider(string path) => _path = path;

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose() => _disposed = true;

    private void Write(string category, LogLevel level, EventId eventId, string message, Exception? exception)
    {
        if (_disposed) return;
        var entry = $"{DateTimeOffset.Now:O} [{level}] {category} ({eventId.Id}) {message}{(exception is null ? string.Empty : Environment.NewLine + exception)}{Environment.NewLine}";
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, entry);
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) provider.Write(category, logLevel, eventId, formatter(state, exception), exception);
        }
    }
}
