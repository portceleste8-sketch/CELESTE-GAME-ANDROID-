namespace Celeste.Core.Services;

/// <summary>
/// Interface para abstrair caminhos de plataforma (Desktop vs Android).
/// Centraliza todas as resoluções de paths no jogo.
/// </summary>
public interface IPlatformPaths
{
    /// <summary>
    /// Diretório raiz do conteúdo instalado (Content.zip extraído).
    /// Desktop: Aqui onde os assets estão
    /// Android: Context.getExternalFilesDir(null)/Celeste/Content/
    /// </summary>
    string ContentRoot { get; }

    /// <summary>
    /// Diretório de saves do usuário.
    /// Desktop: {AppDir}/Saves/
    /// Android: Context.getExternalFilesDir(null)/Celeste/Saves/
    /// </summary>
    string SavesRoot { get; }

    /// <summary>
    /// Diretório de logs.
    /// Desktop: {AppDir}/Logs/
    /// Android: Context.getExternalFilesDir(null)/Celeste/Logs/
    /// </summary>
    string LogsRoot { get; }

    /// <summary>
    /// Diretório temporário para downloads/cache.
    /// Desktop: {AppDir}/Temp/
    /// Android: Context.getCacheDir() ou getExternalCacheDir()
    /// </summary>
    string TempRoot { get; }

    /// <summary>
    /// Resolve um caminho relativo (ex: "Sprites/player.png") para o caminho absoluto no ContentRoot.
    /// </summary>
    string ResolveContentPath(string relativePath);

    /// <summary>
    /// Resolve um caminho relativo de save (ex: "save_0.json") para o caminho absoluto no SavesRoot.
    /// </summary>
    string ResolveSavePath(string relativePath);

    /// <summary>
    /// Resolve um caminho de log com timestamp (ex: "session_2026-01-30_12-30-45.log").
    /// </summary>
    string ResolveLogPath(string filename);

    /// <summary>
    /// Garante que todos os diretórios existem.
    /// </summary>
    void EnsureDirectoriesExist();
}
