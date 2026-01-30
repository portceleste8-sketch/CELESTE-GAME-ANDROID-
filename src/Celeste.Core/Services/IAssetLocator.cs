namespace Celeste.Core.Services;

/// <summary>
/// Interface para localizar e carregar assets do filesystem instalado.
/// Encapsula a resolução de paths e carregamento de arquivos.
/// </summary>
public interface IAssetLocator
{
    /// <summary>
    /// Retorna o caminho completo de um asset no ContentRoot.
    /// </summary>
    string GetAssetPath(string relativeAssetPath);

    /// <summary>
    /// Verifica se um asset existe.
    /// </summary>
    bool AssetExists(string relativeAssetPath);

    /// <summary>
    /// Abre um stream para leitura de um asset.
    /// </summary>
    System.IO.Stream OpenAssetStream(string relativeAssetPath);

    /// <summary>
    /// Lê todo o conteúdo de um asset em bytes.
    /// </summary>
    byte[] ReadAssetBytes(string relativeAssetPath);

    /// <summary>
    /// Lê todo o conteúdo de um asset em texto.
    /// </summary>
    string ReadAssetText(string relativeAssetPath);

    /// <summary>
    /// Valida a presença de assets críticos (checkcontent).
    /// </summary>
    bool ValidateContent(out string[] missingAssets);
}
