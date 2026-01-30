namespace Celeste.Core.Services;

/// <summary>
/// Níveis de log com severidade crescente.
/// </summary>
public enum LogLevel
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

/// <summary>
/// Interface para sistema de logging centralizado.
/// Operacional desde o boot da aplicação.
/// </summary>
public interface ILogSystem
{
    /// <summary>
    /// Inicializa o sistema de logging.
    /// </summary>
    void Initialize(string logsRoot);

    /// <summary>
    /// Escreve uma mensagem de log.
    /// </summary>
    void Log(LogLevel level, string category, string message);

    /// <summary>
    /// Escreve um erro com exceção.
    /// </summary>
    void LogError(string category, Exception ex, string additionalContext = "");

    /// <summary>
    /// Escreve um crash fatal e flush imediato.
    /// </summary>
    void LogCrash(Exception ex, string context = "");

    /// <summary>
    /// Flush de buffers em disco.
    /// </summary>
    void Flush();

    /// <summary>
    /// Retorna o caminho do arquivo de log atual.
    /// </summary>
    string GetCurrentLogFilePath();

    /// <summary>
    /// Retorna todos os caminhos de logs disponíveis.
    /// </summary>
    string[] GetAllLogFiles();
}
