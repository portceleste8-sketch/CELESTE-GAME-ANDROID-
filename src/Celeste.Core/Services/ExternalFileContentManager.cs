using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Core.Services;

/// <summary>
/// Content Manager customizado que carrega XNBs e assets do filesystem instalado.
/// Substitui a dependência de Assembly.Location/ContentDirectory.
/// </summary>
public class ExternalFileContentManager : ContentManager
{
    private readonly IAssetLocator _assetLocator;
    private readonly ILogSystem _logger;
    private readonly string _contentRoot;

    public ExternalFileContentManager(
        IServiceProvider serviceProvider,
        IAssetLocator assetLocator,
        ILogSystem logger,
        string contentRoot)
        : base(serviceProvider)
    {
        _assetLocator = assetLocator;
        _logger = logger;
        _contentRoot = contentRoot;
    }

    /// <summary>
    /// Carrega um asset do filesystem em formato XNB.
    /// </summary>
    public override T Load<T>(string assetName)
    {
        try
        {
            // Normalizar nome do asset (remover extensões, separadores)
            string normalizedName = NormalizeAssetName(assetName);
            
            _logger.Log(LogLevel.Debug, "ContentManager", 
                $"Carregando asset: {assetName} -> {normalizedName}");

            // Tentar carregar como XNB do filesystem
            string xnbPath = $"XNBs/{normalizedName}.xnb";
            
            if (_assetLocator.AssetExists(xnbPath))
            {
                using (var stream = _assetLocator.OpenAssetStream(xnbPath))
                {
                    var result = base.ReadAsset<T>(normalizedName, stream);
                    _logger.Log(LogLevel.Debug, "ContentManager", $"Asset carregado com sucesso: {assetName}");
                    return result;
                }
            }

            _logger.Log(LogLevel.Error, "ContentManager", 
                $"Asset não encontrado: {assetName} (procurou em {xnbPath})");
            throw new ContentLoadException($"Asset não encontrado: {assetName}");
        }
        catch (Exception ex)
        {
            _logger.LogError("ContentManager", ex, $"Erro ao carregar asset: {assetName}");
            throw;
        }
    }

    /// <summary>
    /// Normaliza o nome do asset removendo barras e extensões.
    /// </summary>
    private string NormalizeAssetName(string assetName)
    {
        return assetName
            .Replace("\\", "/")
            .Replace(".xnb", "")
            .Trim();
    }

    /// <summary>
    /// Abre um stream direto para o filesystem (bypass de XNB).
    /// Útil para carregar PNGs e outros assets diretos.
    /// </summary>
    public Stream OpenFileStream(string relativePath)
    {
        try
        {
            return _assetLocator.OpenAssetStream(relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError("ContentManager", ex, $"Erro ao abrir stream: {relativePath}");
            throw;
        }
    }
}
