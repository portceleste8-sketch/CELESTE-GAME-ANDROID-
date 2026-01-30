using System;
using System.Collections.Generic;

namespace Celeste.Core.Services;

/// <summary>
/// Implementação de ILogSystem para todas as plataformas.
/// Estrutura: Logs/YYYY-MM-DD/session_YYYY-MM-DD_HH-mm-ss.log
/// </summary>
public class LogSystemImpl : ILogSystem
{
    private string _logsRoot = "";
    private string _currentSessionFile = "";
    private StreamWriter? _logWriter;
    private readonly object _lockObj = new();
    private readonly Queue<string> _buffer = new();
    private const int BufferFlushInterval = 10; // Flush a cada 10 linhas

    public void Initialize(string logsRoot)
    {
        _logsRoot = logsRoot;
        Directory.CreateDirectory(_logsRoot);

        // Criar pasta de data
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string dateFolder = Path.Combine(_logsRoot, today);
        Directory.CreateDirectory(dateFolder);

        // Criar arquivo de sessão com timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _currentSessionFile = Path.Combine(dateFolder, $"session_{timestamp}.log");

        try
        {
            _logWriter = new StreamWriter(_currentSessionFile, true)
            {
                AutoFlush = false
            };

            Log(LogLevel.Info, "LogSystem", $"LogSystem inicializado em {_currentSessionFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar LogSystem: {ex.Message}");
        }
    }

    public void Log(LogLevel level, string category, string message)
    {
        lock (_lockObj)
        {
            if (_logWriter == null) return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logLine = $"[{timestamp}] [{level}] [{category}] {message}";

            _buffer.Enqueue(logLine);

            // Flush periódico
            if (_buffer.Count >= BufferFlushInterval)
            {
                Flush();
            }
        }
    }

    public void LogError(string category, Exception ex, string additionalContext = "")
    {
        string message = $"Exception: {ex.GetType().Name}: {ex.Message}";
        if (!string.IsNullOrEmpty(additionalContext))
            message += $" | Context: {additionalContext}";
        
        Log(LogLevel.Error, category, message);
        
        if (!string.IsNullOrEmpty(ex.StackTrace))
            Log(LogLevel.Debug, category, $"StackTrace: {ex.StackTrace}");

        if (ex.InnerException != null)
            LogError(category, ex.InnerException, "Inner exception");
    }

    public void LogCrash(Exception ex, string context = "")
    {
        lock (_lockObj)
        {
            Log(LogLevel.Fatal, "CRASH", $"Aplicação crashou: {context}");
            LogError("CRASH", ex, "Fatal error");
            Flush();

            // Tentar criar arquivo de crash log separado
            try
            {
                string crashFile = Path.Combine(_logsRoot, $"crash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
                using (var sw = new StreamWriter(crashFile))
                {
                    sw.WriteLine($"=== CRASH LOG ===");
                    sw.WriteLine($"Timestamp: {DateTime.Now}");
                    sw.WriteLine($"Context: {context}");
                    sw.WriteLine($"Exception: {ex}");
                    sw.WriteLine($"StackTrace: {ex.StackTrace}");
                }
            }
            catch { }
        }
    }

    public void Flush()
    {
        lock (_lockObj)
        {
            if (_logWriter == null) return;

            try
            {
                while (_buffer.Count > 0)
                {
                    _logWriter.WriteLine(_buffer.Dequeue());
                }
                _logWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer flush de logs: {ex.Message}");
            }
        }
    }

    public string GetCurrentLogFilePath()
    {
        return _currentSessionFile;
    }

    public string[] GetAllLogFiles()
    {
        try
        {
            if (!Directory.Exists(_logsRoot))
                return Array.Empty<string>();

            return Directory.GetFiles(_logsRoot, "*.log", SearchOption.AllDirectories);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public void Dispose()
    {
        Flush();
        _logWriter?.Dispose();
        _logWriter = null;
    }

    ~LogSystemImpl()
    {
        Dispose();
    }
}
