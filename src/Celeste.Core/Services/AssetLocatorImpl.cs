using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Celeste.Core.Services;

/// <summary>
/// Implementação de IAssetLocator para validar e carregar assets do filesystem.
/// </summary>
public class AssetLocatorImpl : IAssetLocator
{
    private readonly IPlatformPaths _paths;
    private readonly ILogSystem _logger;

    // Assets críticos que devem estar presentes
    private static readonly string[] CRITICAL_ASSETS = new[]
    {
        "Dialog",
        "Fonts",
        "Effects",
        "Atlases",
    };

    public AssetLocatorImpl(IPlatformPaths paths, ILogSystem logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public string GetAssetPath(string relativeAssetPath)
    {
        return _paths.ResolveContentPath(relativeAssetPath);
    }

    public bool AssetExists(string relativeAssetPath)
    {
        string fullPath = GetAssetPath(relativeAssetPath);
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    public System.IO.Stream OpenAssetStream(string relativeAssetPath)
    {
        string fullPath = GetAssetPath(relativeAssetPath);
        
        if (!File.Exists(fullPath))
        {
            _logger.Log(LogLevel.Error, "AssetLocator", $"Asset não encontrado: {relativeAssetPath} ({fullPath})");
            throw new FileNotFoundException($"Asset não encontrado: {relativeAssetPath}", fullPath);
        }

        try
        {
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception ex)
        {
            _logger.LogError("AssetLocator", ex, $"Erro ao abrir stream para {relativeAssetPath}");
            throw;
        }
    }

    public byte[] ReadAssetBytes(string relativeAssetPath)
    {
        string fullPath = GetAssetPath(relativeAssetPath);
        
        if (!File.Exists(fullPath))
        {
            _logger.Log(LogLevel.Error, "AssetLocator", $"Asset não encontrado: {relativeAssetPath}");
            throw new FileNotFoundException($"Asset não encontrado: {relativeAssetPath}");
        }

        try
        {
            return File.ReadAllBytes(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError("AssetLocator", ex, $"Erro ao ler bytes de {relativeAssetPath}");
            throw;
        }
    }

    public string ReadAssetText(string relativeAssetPath)
    {
        string fullPath = GetAssetPath(relativeAssetPath);
        
        if (!File.Exists(fullPath))
        {
            _logger.Log(LogLevel.Error, "AssetLocator", $"Asset não encontrado: {relativeAssetPath}");
            throw new FileNotFoundException($"Asset não encontrado: {relativeAssetPath}");
        }

        try
        {
            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError("AssetLocator", ex, $"Erro ao ler texto de {relativeAssetPath}");
            throw;
        }
    }

    public bool ValidateContent(out string[] missingAssets)
    {
        var missing = new List<string>();

        foreach (var asset in CRITICAL_ASSETS)
        {
            string assetPath = _paths.ResolveContentPath(asset);
            if (!Directory.Exists(assetPath))
            {
                missing.Add(asset);
                _logger.Log(LogLevel.Warning, "AssetLocator", $"Asset crítico não encontrado: {asset}");
            }
        }

        // Validar FMOD banks
        string fmodPath = _paths.ResolveContentPath("FMOD");
        if (!Directory.Exists(fmodPath))
        {
            missing.Add("FMOD");
            _logger.Log(LogLevel.Warning, "AssetLocator", "Diretório FMOD não encontrado");
        }

        if (missing.Count == 0)
        {
            _logger.Log(LogLevel.Info, "AssetLocator", "Validação de content OK - todos os assets críticos presentes");
        }
        else
        {
            _logger.Log(LogLevel.Warning, "AssetLocator", $"Validação falhou - faltam {missing.Count} assets");
        }

        missingAssets = missing.ToArray();
        return missing.Count == 0;
    }
}
