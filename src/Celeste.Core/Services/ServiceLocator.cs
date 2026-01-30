namespace Celeste.Core.Services;

/// <summary>
/// Service Locator centralizado para a aplicação.
/// Fornece acesso singleton às interfaces de serviço.
/// </summary>
public static class ServiceLocator
{
    private static IPlatformPaths? _platformPaths;
    private static ILogSystem? _logSystem;
    private static IAssetLocator? _assetLocator;

    /// <summary>
    /// Inicializa o service locator com implementações concretas.
    /// Deve ser chamado no boot da aplicação antes de qualquer uso de serviços.
    /// </summary>
    public static void Initialize(IPlatformPaths platformPaths, ILogSystem logSystem)
    {
        _platformPaths = platformPaths ?? throw new ArgumentNullException(nameof(platformPaths));
        _logSystem = logSystem ?? throw new ArgumentNullException(nameof(logSystem));
        _assetLocator = new AssetLocatorImpl(platformPaths, logSystem);
    }

    /// <summary>
    /// Obtém a interface de paths de plataforma.
    /// </summary>
    public static IPlatformPaths GetPlatformPaths()
    {
        return _platformPaths ?? throw new InvalidOperationException(
            "ServiceLocator não foi inicializado. Chamar Initialize() no boot.");
    }

    /// <summary>
    /// Obtém a interface de logging.
    /// </summary>
    public static ILogSystem GetLogger()
    {
        return _logSystem ?? throw new InvalidOperationException(
            "ServiceLocator não foi inicializado. Chamar Initialize() no boot.");
    }

    /// <summary>
    /// Obtém a interface de localização de assets.
    /// </summary>
    public static IAssetLocator GetAssetLocator()
    {
        return _assetLocator ?? throw new InvalidOperationException(
            "ServiceLocator não foi inicializado. Chamar Initialize() no boot.");
    }

    /// <summary>
    /// Reseta o service locator (útil para testes).
    /// </summary>
    public static void Reset()
    {
        _platformPaths = null;
        _logSystem = null;
        _assetLocator = null;
    }
}
